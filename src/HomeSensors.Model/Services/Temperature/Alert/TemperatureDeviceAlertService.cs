using HomeSensors.Model.Emailing;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories;
using Microsoft.Extensions.Logging;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Services.Temperature.Alert;

public class TemperatureDeviceAlertService
{
    private readonly ILogger<TemperatureDeviceAlertService> _logger;
    private readonly TemperatureLocationRepository _locationRepository;
    private readonly TemperatureDeviceRepository _deviceRepository;
    private readonly EmailNotificationService _emailNotificationService;
    private readonly TemperatureAlertsSettings _alertSettings;

    public TemperatureDeviceAlertService(ILogger<TemperatureDeviceAlertService> logger,
        TemperatureLocationRepository locationRepository, TemperatureDeviceRepository deviceRepository,
        EmailNotificationService emailNotificationService, TemperatureAlertsSettings alertSettings)
    {
        _logger = logger;
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
        _emailNotificationService = emailNotificationService;
        _alertSettings = alertSettings;
    }

    public async Task Process(List<TemperatureDeviceAlert> latchedAlerts, DateTimeOffset now, CancellationToken stoppingToken)
    {
        var devices = await _deviceRepository.GetAll();

        var locations = await _locationRepository.GetAll();

        var betweenNotifications = TimeSpan.FromMinutes(_alertSettings.BetweenNotificationsMinutes);
        var resendAfter = now.Add(betweenNotifications);

        var inactiveAlerts = devices
            .Where(d => d.IsInactive)
            .Select(d => new TemperatureDeviceAlert(
                TemperatureDeviceAlertType.DeviceInactive,
                d,
                locations.Find(l => l.Id == d.LocationId),
                resendAfter));

        var lowBatteryAlert = devices
            .Where(d => d.IsBatteryLevelLow)
            .Select(d => new TemperatureDeviceAlert(
                TemperatureDeviceAlertType.DeviceLowBattery,
                d,
                locations.Find(l => l.Id == d.LocationId),
                resendAfter));

        var currentAlerts = inactiveAlerts
            .Concat(lowBatteryAlert)
            .ToList();

        // Clear alerts that are no longer current.
        var clearedAlerts = latchedAlerts
            .Where(l => !currentAlerts.Contains(l))
            .ToList();

        foreach (var alert in clearedAlerts)
        {
            switch (alert.Type)
            {
                case TemperatureDeviceAlertType.DeviceInactive:
                    await NotifyInactiveClear(alert, stoppingToken);
                    break;

                case TemperatureDeviceAlertType.DeviceLowBattery:
                    await NotifyLowBatteryClear(alert, stoppingToken);
                    break;
            }

            latchedAlerts.Remove(alert);
        }

        // Remove alerts that need resent. They will be resent when re-added from current.
        latchedAlerts.RemoveAll(x => x.ResendAfter <= now);

        var alertsToSend = currentAlerts
            .Where(c => !latchedAlerts.Contains(c))
            .ToList();

        foreach (var alert in alertsToSend)
        {
            switch (alert.Type)
            {
                case TemperatureDeviceAlertType.DeviceInactive:
                    await NotifyInactiveAlert(alert, stoppingToken);
                    break;

                case TemperatureDeviceAlertType.DeviceLowBattery:
                    await NotifyLowBatteryAlert(alert, stoppingToken);
                    break;
            }

            latchedAlerts.Add(alert);
        }
    }

    private Task NotifyInactiveAlert(TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} is inactive";
        var body = $"The temperature sensor in {locationName} hasn't sent a reading in a while.";

        _logger.LogWarning("Alert: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task NotifyInactiveClear(TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} is active again";
        var body = $"The temperature sensor in {locationName} is sending readings again.";

        _logger.LogWarning("Alert clear: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task NotifyLowBatteryAlert(TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} has a low battery";
        var body = $"The temperature sensor in {locationName} has a low battery.";

        _logger.LogWarning("Alert: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task NotifyLowBatteryClear(TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} no longer has a low battery";
        var body = $"The temperature sensor in {locationName} no longer has a low battery.";

        _logger.LogWarning("Alert clear: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task SendEmail(string subject, string body, TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var device = alert.Device;
        var deviceName = $"{device.Name} ({device.MqttTopic})";
        var readingTempString = TemperatureHelpers.GetDualTempString(alert.Device.LastReading?.TemperatureCelsius);
        var readingTime = alert.Device.LastReading?.Time;

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject(subject);

            const string indent = "&nbsp;&nbsp;&nbsp;&nbsp;";

            e.AddLine(body);
            e.AddLine();
            e.AddLine("Device:");
            e.AddLine($"{indent}Name (topic): {deviceName}");
            e.AddLine($"{indent}Last reading:");
            e.AddLine($"{indent}{indent}Temperature: {readingTempString}");
            e.AddLine($"{indent}{indent}Time: {readingTime}");
        }, stoppingToken);
    }
}
