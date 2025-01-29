using HomeSensors.Model.Emailing;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using VoidCore.Model.Text;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Services.WaterLeak;

public class WaterLeakAlertService
{
    private readonly ILogger<WaterLeakAlertService> _logger;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly MqttWaterLeaksSettings _mqttWaterLeaksSettings;
    private readonly ConcurrentDictionary<WaterLeakDeviceAlert, DateTimeOffset> _latchedMessageAlerts = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastCheckIns = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _latchedInactiveAlerts = new();

    public WaterLeakAlertService(ILogger<WaterLeakAlertService> logger, IDateTimeService dateTimeService, EmailNotificationService emailNotificationService,
        MqttWaterLeaksSettings mqttWaterLeaksSettings)
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
        _emailNotificationService = emailNotificationService;
        _mqttWaterLeaksSettings = mqttWaterLeaksSettings;

        var now = _dateTimeService.MomentWithOffset;

        foreach (var sensor in mqttWaterLeaksSettings.Devices)
        {
            _lastCheckIns.TryAdd(sensor.Name, now);
        }
    }

    public async Task ProcessAsync(MqttWaterLeakDeviceMessage message)
    {
        var now = _dateTimeService.MomentWithOffset;

        // Clear expired latched alerts so they can be sent again.
        var expiredAlerts = _latchedMessageAlerts
            .Where(x => x.Value.AddMinutes(_mqttWaterLeaksSettings.BetweenNotificationsMinutes) < now)
            .ToArray();

        foreach (var expiredAlert in expiredAlerts)
        {
            // Note that we're not sending a clear notice.
            _latchedMessageAlerts.TryRemove(expiredAlert);
        }

        if (!message.LocationName.IsNullOrWhiteSpace())
        {
            var lastCheckInExists = _lastCheckIns.TryGetValue(message.LocationName, out var lastCheckInTime);

            // If the last check-in was expired send clear notification.
            if (lastCheckInExists && lastCheckInTime.AddMinutes(_mqttWaterLeaksSettings.InactiveDeviceLimitMinutes) < now)
            {
                await NotifyActiveAsync(message, now);
            }

            _lastCheckIns.AddOrUpdate(message.LocationName, now, (_, _) => now);
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

        // Clear expired latched alerts so they can be sent again.
        var expiredAlerts = _latchedInactiveAlerts
            .Where(x => x.Value.AddMinutes(_mqttWaterLeaksSettings.BetweenNotificationsMinutes) < now)
            .ToArray();

        foreach (var expiredAlert in expiredAlerts)
        {
            _latchedInactiveAlerts.TryRemove(expiredAlert);
        }

        var inactiveDevices = _lastCheckIns
            .Where(x => x.Value.AddMinutes(_mqttWaterLeaksSettings.InactiveDeviceLimitMinutes) < now
                && !_latchedInactiveAlerts.ContainsKey(x.Key))
            .ToArray();

        foreach (var inactiveDevice in inactiveDevices)
        {
            _latchedInactiveAlerts.TryAdd(inactiveDevice.Key, now);
            await NotifyInactiveAsync(inactiveDevice, now);
        }
    }

    private async Task NotifyLeakAsync(MqttWaterLeakDeviceMessage message, DateTimeOffset now)
    {
        var alert = new WaterLeakDeviceAlert(message.LocationName, WaterLeakDeviceAlertType.WaterLeak);

        // If there is ongoing alert, don't resend.
        if (_latchedMessageAlerts.ContainsKey(alert))
        {
            return;
        }

        _latchedMessageAlerts.AddOrUpdate(alert, now, (_, _) => now);

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
        if (_latchedMessageAlerts.ContainsKey(alert))
        {
            return;
        }

        _latchedMessageAlerts.AddOrUpdate(alert, now, (_, _) => now);

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
