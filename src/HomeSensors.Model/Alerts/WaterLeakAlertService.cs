using HomeSensors.Model.Emailing;
using HomeSensors.Model.Mqtt;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Alerts;

public class WaterLeakAlertService
{
    private readonly ILogger<WaterLeakAlertService> _logger;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly List<WaterLeakAlert> _waterLeakAlerts = new();

    public WaterLeakAlertService(ILogger<WaterLeakAlertService> logger, IDateTimeService dateTimeService, EmailNotificationService emailNotificationService)
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
        _emailNotificationService = emailNotificationService;
    }

    public async Task Process(MqttWaterLeakMessage message, int betweenNotificationsMinutes)
    {
        var now = _dateTimeService.MomentWithOffset;

        ClearExpiredAlerts(now, betweenNotificationsMinutes);

        switch (message.Payload.Water_Leak)
        {
            case null:
                var subject = $"{nameof(MqttWaterLeakMessagePayload.Water_Leak)} property not found in payload";
                var body = $"Serialized payload object: {JsonSerializer.Serialize(message)}";
                await _emailNotificationService.NotifyError(body, subject);
                break;
            case true:
                await NotifyLeak(message, now);
                break;
        }

        if (message.Payload.Battery_Low == true)
        {
            await NotifyBatteryLow(message, now);
        }
    }

    private async Task NotifyLeak(MqttWaterLeakMessage message, DateTimeOffset now)
    {
        var type = WaterLeakAlertType.WaterLeak;

        // If there is ongoing alert, don't resend.
        if (_waterLeakAlerts.Exists(x => x.LocationName == message.LocationName && x.Type == type))
        {
            return;
        }

        _waterLeakAlerts.Add(new(message.LocationName, type, now));

        _logger.LogWarning("Water leak detected: {LocationName}", message.LocationName);

        await _emailNotificationService.Send(e =>
        {
            e.SetSubject($"Water leak detected: {message.LocationName}");

            e.AddLine($"Water leak detected at {message.LocationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
            e.AddLine($"Battery: {message.Payload.Battery}%");
        }, default);
    }

    private async Task NotifyBatteryLow(MqttWaterLeakMessage message, DateTimeOffset now)
    {
        var type = WaterLeakAlertType.LowBattery;

        // If there is ongoing alert, don't resend.
        if (_waterLeakAlerts.Exists(x => x.LocationName == message.LocationName && x.Type == type))
        {
            return;
        }

        _waterLeakAlerts.Add(new(message.LocationName, type, now));

        _logger.LogWarning("Water leak sensor battery low: {LocationName}", message.LocationName);

        await _emailNotificationService.Send(e =>
        {
            e.SetSubject($"Water leak sensor battery low: {message.LocationName}");

            e.AddLine($"Water leak sensor battery low at {message.LocationName}.");
            e.AddLine();
            e.AddLine($"Time: {now}");
            e.AddLine($"Battery: {message.Payload.Battery}%");
        }, default);
    }

    private void ClearExpiredAlerts(DateTimeOffset now, int betweenNotificationsMinutes)
    {
        // Note that we're not sending a clear notice.
        _waterLeakAlerts.RemoveAll(x => x.TimeNotified.AddMinutes(betweenNotificationsMinutes) < now);
    }
}

public record WaterLeakAlert(string LocationName, WaterLeakAlertType Type, DateTimeOffset TimeNotified);

public enum WaterLeakAlertType
{
    WaterLeak,
    LowBattery
}
