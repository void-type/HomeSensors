using HomeSensors.Model.Data;
using HomeSensors.Model.Infrastructure.Emailing;
using HomeSensors.Model.WaterLeak.Models;
using HomeSensors.Model.WaterLeak.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoidCore.Model.Text;

namespace HomeSensors.Model.WaterLeak.Services;

public class WaterLeakAlertService(ILogger<WaterLeakAlertService> logger, TimeProvider timeProvider, EmailNotificationService emailNotificationService,
    MqttWaterLeaksSettings mqttWaterLeaksSettings, WaterLeakAlertState state, HomeSensorsContext data)
{
    public async Task ProcessAsync(MqttWaterLeakDeviceMessage message)
    {
        var now = timeProvider.GetUtcNow();

        // Clear expired latched alerts so they can be sent again.
        var expiredAlerts = state.LatchedMessageAlerts
            .Where(x => x.Value.AddMinutes(mqttWaterLeaksSettings.BetweenNotificationsMinutes) < now)
            .ToArray();

        foreach (var expiredAlert in expiredAlerts)
        {
            // Note that we're not sending a clear notice.
            state.LatchedMessageAlerts.TryRemove(expiredAlert);
        }

        if (!message.LocationName.IsNullOrWhiteSpace())
        {
            var lastCheckInExists = state.LastCheckIns.TryGetValue(message.LocationName, out var lastCheckInTime);

            // If the last check-in was expired send clear notification.
            if (lastCheckInExists && message.InactiveLimitMinutes > 0 && lastCheckInTime.AddMinutes(message.InactiveLimitMinutes) < now)
            {
                await NotifyActiveAsync(message, now);
            }

            state.LastCheckIns.AddOrUpdate(message.LocationName, now, (_, _) => now);
        }

        switch (message.Payload.Water_Leak)
        {
            case null:
                throw new InvalidOperationException($"Payload property \"{nameof(MqttWaterLeakDeviceMessagePayload)}.{nameof(MqttWaterLeakDeviceMessagePayload.Water_Leak)}\" not found or was null.");
            case true:
                await NotifyLeakAsync(message, now);
                break;
        }

        if (message.Payload.Battery_Low == true)
        {
            await NotifyBatteryLowAsync(message, now);
        }
    }

    public async Task CheckInactiveAsync()
    {
        var now = timeProvider.GetUtcNow();

        // Sync device names from DB into LastCheckIns.
        var devices = await data.WaterLeakDevices
            .TagWith($"Query called from {nameof(WaterLeakAlertService)}.{nameof(CheckInactiveAsync)}.")
            .AsNoTracking()
            .Select(x => new { x.Name, x.InactiveLimitMinutes })
            .ToListAsync();

        var deviceLimits = devices.ToDictionary(x => x.Name, x => x.InactiveLimitMinutes);

        foreach (var device in devices)
        {
            state.LastCheckIns.TryAdd(device.Name, now);
        }

        // Remove stale entries that no longer exist in DB.
        var staleKeys = state.LastCheckIns.Keys
            .Where(k => !deviceLimits.ContainsKey(k))
            .ToArray();

        foreach (var key in staleKeys)
        {
            state.LastCheckIns.TryRemove(key, out _);
        }

        // Clear expired latched alerts so they can be sent again.
        var expiredAlerts = state.LatchedInactiveAlerts
            .Where(x => x.Value.AddMinutes(mqttWaterLeaksSettings.BetweenNotificationsMinutes) < now)
            .ToArray();

        foreach (var expiredAlert in expiredAlerts)
        {
            state.LatchedInactiveAlerts.TryRemove(expiredAlert);
        }

        var inactiveDevices = state.LastCheckIns
            .Where(x => deviceLimits.TryGetValue(x.Key, out var limit)
                && limit > 0
                && x.Value.AddMinutes(limit) < now
                && !state.LatchedInactiveAlerts.ContainsKey(x.Key))
            .ToArray();

        foreach (var inactiveDevice in inactiveDevices)
        {
            state.LatchedInactiveAlerts.TryAdd(inactiveDevice.Key, now);
            await NotifyInactiveAsync(inactiveDevice, now);
        }
    }

    private async Task NotifyLeakAsync(MqttWaterLeakDeviceMessage message, DateTimeOffset now)
    {
        var alert = new WaterLeakDeviceAlert(message.LocationName, WaterLeakDeviceAlertType.WaterLeak);

        // If there is ongoing alert, don't resend.
        if (state.LatchedMessageAlerts.ContainsKey(alert))
        {
            return;
        }

        state.LatchedMessageAlerts.AddOrUpdate(alert, now, (_, _) => now);

        logger.LogWarning("Water leak detected: {LocationName}", message.LocationName);

        await emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"Water leak detected: {message.LocationName}");

            e.AddLine($"Water leak detected at {message.LocationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
            e.AddLine($"Battery: {message.Payload.Battery}%");
        }, default);
    }

    private async Task NotifyBatteryLowAsync(MqttWaterLeakDeviceMessage message, DateTimeOffset now)
    {
        var alert = new WaterLeakDeviceAlert(message.LocationName, WaterLeakDeviceAlertType.LowBattery);

        // If there is ongoing alert, don't resend.
        if (state.LatchedMessageAlerts.ContainsKey(alert))
        {
            return;
        }

        state.LatchedMessageAlerts.AddOrUpdate(alert, now, (_, _) => now);

        logger.LogWarning("Water leak sensor battery low: {LocationName}", message.LocationName);

        await emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"Water leak sensor battery low: {message.LocationName}");

            e.AddLine($"Water leak sensor battery low at {message.LocationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
            e.AddLine($"Battery: {message.Payload.Battery}%");
        }, default);
    }

    private async Task NotifyInactiveAsync(KeyValuePair<string, DateTimeOffset> inactiveDevice, DateTimeOffset now)
    {
        var locationName = inactiveDevice.Key;

        logger.LogWarning("Water leak sensor inactive: {LocationName}", locationName);

        await emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"Water leak sensor inactive: {locationName}");

            e.AddLine($"Water leak sensor inactive at {locationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
            e.AddLine($"Last seen: {inactiveDevice.Value}");
        }, default);
    }

    private async Task NotifyActiveAsync(MqttWaterLeakDeviceMessage message, DateTimeOffset now)
    {
        var locationName = message.LocationName;

        logger.LogWarning("Water leak sensor active again: {LocationName}", locationName);

        await emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"Water leak sensor active again: {locationName}");

            e.AddLine($"Water leak sensor active again at {locationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
        }, default);
    }
}
