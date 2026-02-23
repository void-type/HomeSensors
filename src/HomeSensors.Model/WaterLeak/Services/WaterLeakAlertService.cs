using HomeSensors.Model.Data;
using HomeSensors.Model.Infrastructure.Emailing;
using HomeSensors.Model.WaterLeak.Models;
using HomeSensors.Model.WaterLeak.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoidCore.Model.Text;
using VoidCore.Model.Time;

namespace HomeSensors.Model.WaterLeak.Services;

public class WaterLeakAlertService
{
    private readonly ILogger<WaterLeakAlertService> _logger;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly MqttWaterLeaksSettings _mqttWaterLeaksSettings;
    private readonly WaterLeakAlertState _state;
    private readonly HomeSensorsContext _data;

    public WaterLeakAlertService(ILogger<WaterLeakAlertService> logger, IDateTimeService dateTimeService, EmailNotificationService emailNotificationService,
        MqttWaterLeaksSettings mqttWaterLeaksSettings, WaterLeakAlertState state, HomeSensorsContext data)
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
        _emailNotificationService = emailNotificationService;
        _mqttWaterLeaksSettings = mqttWaterLeaksSettings;
        _state = state;
        _data = data;
    }

    public async Task ProcessAsync(MqttWaterLeakDeviceMessage message)
    {
        var now = _dateTimeService.MomentWithOffset;

        // Clear expired latched alerts so they can be sent again.
        var expiredAlerts = _state.LatchedMessageAlerts
            .Where(x => x.Value.AddMinutes(_mqttWaterLeaksSettings.BetweenNotificationsMinutes) < now)
            .ToArray();

        foreach (var expiredAlert in expiredAlerts)
        {
            // Note that we're not sending a clear notice.
            _state.LatchedMessageAlerts.TryRemove(expiredAlert);
        }

        if (!message.LocationName.IsNullOrWhiteSpace())
        {
            var lastCheckInExists = _state.LastCheckIns.TryGetValue(message.LocationName, out var lastCheckInTime);

            // If the last check-in was expired send clear notification.
            if (lastCheckInExists && lastCheckInTime.AddMinutes(_mqttWaterLeaksSettings.InactiveDeviceLimitMinutes) < now)
            {
                await NotifyActiveAsync(message, now);
            }

            _state.LastCheckIns.AddOrUpdate(message.LocationName, now, (_, _) => now);
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
        var now = _dateTimeService.MomentWithOffset;

        // Sync device names from DB into LastCheckIns.
        var deviceNames = await _data.WaterLeakDevices
            .TagWith($"Query called from {nameof(WaterLeakAlertService)}.{nameof(CheckInactiveAsync)}.")
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var name in deviceNames)
        {
            _state.LastCheckIns.TryAdd(name, now);
        }

        // Remove stale entries that no longer exist in DB.
        var deviceNameSet = deviceNames.ToHashSet();
        var staleKeys = _state.LastCheckIns.Keys
            .Where(k => !deviceNameSet.Contains(k))
            .ToArray();

        foreach (var key in staleKeys)
        {
            _state.LastCheckIns.TryRemove(key, out _);
        }

        // Clear expired latched alerts so they can be sent again.
        var expiredAlerts = _state.LatchedInactiveAlerts
            .Where(x => x.Value.AddMinutes(_mqttWaterLeaksSettings.BetweenNotificationsMinutes) < now)
            .ToArray();

        foreach (var expiredAlert in expiredAlerts)
        {
            _state.LatchedInactiveAlerts.TryRemove(expiredAlert);
        }

        var inactiveDevices = _state.LastCheckIns
            .Where(x => x.Value.AddMinutes(_mqttWaterLeaksSettings.InactiveDeviceLimitMinutes) < now
                && !_state.LatchedInactiveAlerts.ContainsKey(x.Key))
            .ToArray();

        foreach (var inactiveDevice in inactiveDevices)
        {
            _state.LatchedInactiveAlerts.TryAdd(inactiveDevice.Key, now);
            await NotifyInactiveAsync(inactiveDevice, now);
        }
    }

    private async Task NotifyLeakAsync(MqttWaterLeakDeviceMessage message, DateTimeOffset now)
    {
        var alert = new WaterLeakDeviceAlert(message.LocationName, WaterLeakDeviceAlertType.WaterLeak);

        // If there is ongoing alert, don't resend.
        if (_state.LatchedMessageAlerts.ContainsKey(alert))
        {
            return;
        }

        _state.LatchedMessageAlerts.AddOrUpdate(alert, now, (_, _) => now);

        _logger.LogWarning("Water leak detected: {LocationName}", message.LocationName);

        await _emailNotificationService.SendAsync(e =>
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
        if (_state.LatchedMessageAlerts.ContainsKey(alert))
        {
            return;
        }

        _state.LatchedMessageAlerts.AddOrUpdate(alert, now, (_, _) => now);

        _logger.LogWarning("Water leak sensor battery low: {LocationName}", message.LocationName);

        await _emailNotificationService.SendAsync(e =>
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

        _logger.LogWarning("Water leak sensor inactive: {LocationName}", locationName);

        await _emailNotificationService.SendAsync(e =>
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

        _logger.LogWarning("Water leak sensor active again: {LocationName}", locationName);

        await _emailNotificationService.SendAsync(e =>
        {
            e.SetSubject($"Water leak sensor active again: {locationName}");

            e.AddLine($"Water leak sensor active again at {locationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
        }, default);
    }
}
