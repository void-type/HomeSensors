using HomeSensors.Model.Emailing;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories;
using Microsoft.Extensions.Logging;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Alerts;

public class AlertDevicesService
{
    private readonly ILogger<AlertTemperatureLimitsService> _logger;
    private readonly TemperatureLocationRepository _locationRepository;
    private readonly TemperatureDeviceRepository _deviceRepository;
    private readonly EmailNotificationService _emailNotificationService;

    public AlertDevicesService(ILogger<AlertTemperatureLimitsService> logger,
        TemperatureLocationRepository locationRepository, TemperatureDeviceRepository deviceRepository,
        EmailNotificationService emailNotificationService)
    {
        _logger = logger;
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
        _emailNotificationService = emailNotificationService;
    }

    public async Task Process(List<DeviceAlert> latchedAlerts, DateTimeOffset now, TimeSpan betweenAlerts, CancellationToken stoppingToken)
    {
        var devices = await _deviceRepository.GetAll();

        var locations = await _locationRepository.GetAll();

        var resendAfter = now.Add(betweenAlerts);

        var inactiveAlerts = devices
            .Where(d => d.IsInactive)
            .Select(d => new DeviceAlert(
                DeviceAlertType.DeviceInactive,
                d,
                locations.Find(l => l.Id == d.CurrentLocationId),
                resendAfter));

        var lowBatteryAlert = devices
            .Where(d => d.IsBatteryLevelLow)
            .Select(d => new DeviceAlert(
                DeviceAlertType.DeviceLowBattery,
                d,
                locations.Find(l => l.Id == d.CurrentLocationId),
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
                case DeviceAlertType.DeviceInactive:
                    await NotifyInactiveClear(alert, stoppingToken);
                    break;

                case DeviceAlertType.DeviceLowBattery:
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
                case DeviceAlertType.DeviceInactive:
                    await NotifyInactiveAlert(alert, stoppingToken);
                    break;

                case DeviceAlertType.DeviceLowBattery:
                    await NotifyLowBatteryAlert(alert, stoppingToken);
                    break;
            }

            latchedAlerts.Add(alert);
        }
    }

    private Task NotifyInactiveAlert(DeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} temperature sensor is inactive";
        var body = $"{locationName} hasn't sent a reading in a while.";

        _logger.LogWarning("Alert: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task NotifyInactiveClear(DeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} temperature sensor is no longer inactive";
        var body = $"{locationName} became active again.";

        _logger.LogWarning("Alert clear: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task NotifyLowBatteryAlert(DeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} temperature sensor has a low battery";
        var body = $"{locationName} has a low battery.";

        _logger.LogWarning("Alert: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task NotifyLowBatteryClear(DeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} temperature sensor no longer has a low battery";
        var body = $"{locationName} no longer has a low battery.";

        _logger.LogWarning("Alert clear: {Subject}", subject);

        return SendEmail(subject, body, alert, stoppingToken);
    }

    private Task SendEmail(string subject, string body, DeviceAlert alert, CancellationToken stoppingToken)
    {
        var device = alert.Device;
        var deviceName = $"{device.DeviceModel}/{device.DeviceId}/{device.DeviceChannel}";
        var readingTempString = TemperatureHelpers.GetDualTempString(alert.Device.LastReading?.TemperatureCelsius);
        var readingTime = alert.Device.LastReading?.Time;

        return _emailNotificationService.Send(e =>
        {
            e.SetSubject(subject);

            e.AddLine(body);
            e.AddLine();
            e.AddLine($"Device: {deviceName}");
            e.AddLine();
            e.AddLine("Last reading:");
            e.AddLine($"    Temperature: {readingTempString}");
            e.AddLine($"    Time: {readingTime}");
        }, stoppingToken);
    }
}
