using HomeSensors.Model.Infrastructure.Emailing;
using HomeSensors.Model.Temperature.Helpers;
using HomeSensors.Model.Temperature.Models;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Model.Temperature.Workers;
using Microsoft.Extensions.Logging;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Temperature.Services;

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

    public async Task ProcessAsync(List<TemperatureDeviceAlert> latchedAlerts, DateTimeOffset now, CancellationToken stoppingToken)
    {
        var devices = await _deviceRepository.GetAllAsync();

        var locations = await _locationRepository.GetAllAsync();

        var betweenNotifications = TimeSpan.FromMinutes(_alertSettings.BetweenNotificationsMinutes);
        var resendAfter = now.Add(betweenNotifications);

        var inactiveAlerts = devices
            .Where(d => d.IsInactive && !d.ExcludeFromInactiveAlerts)
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
                    await NotifyInactiveClearAsync(alert, devices, stoppingToken);
                    break;

                case TemperatureDeviceAlertType.DeviceLowBattery:
                    await NotifyLowBatteryClearAsync(alert, devices, stoppingToken);
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
                    await NotifyInactiveAlertAsync(alert, stoppingToken);
                    break;

                case TemperatureDeviceAlertType.DeviceLowBattery:
                    await NotifyLowBatteryAlertAsync(alert, stoppingToken);
                    break;
            }

            latchedAlerts.Add(alert);
        }
    }

    private async Task NotifyInactiveAlertAsync(TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} is inactive";
        var body = $"The temperature sensor in {locationName} hasn't sent a reading in a while.";

        _logger.LogWarning("Alert: {Subject}", subject);

        await SendEmailAsync(subject, body, alert.Device, stoppingToken);
    }

    private async Task NotifyInactiveClearAsync(TemperatureDeviceAlert alert, List<TemperatureDeviceResponse> devices, CancellationToken stoppingToken)
    {
        var device = devices.Find(d => d.Id == alert.Device.Id) ?? alert.Device;
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} is active again";
        var body = $"The temperature sensor in {locationName} is sending readings again.";

        _logger.LogWarning("Alert clear: {Subject}", subject);

        await SendEmailAsync(subject, body, device, stoppingToken);
    }

    private async Task NotifyLowBatteryAlertAsync(TemperatureDeviceAlert alert, CancellationToken stoppingToken)
    {
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} has a low battery";
        var body = $"The temperature sensor in {locationName} has a low battery.";

        _logger.LogWarning("Alert: {Subject}", subject);

        await SendEmailAsync(subject, body, alert.Device, stoppingToken);
    }

    private async Task NotifyLowBatteryClearAsync(TemperatureDeviceAlert alert, List<TemperatureDeviceResponse> devices, CancellationToken stoppingToken)
    {
        var device = devices.Find(d => d.Id == alert.Device.Id) ?? alert.Device;
        var locationName = alert.Location?.Name ?? "Unknown";

        var subject = $"{locationName} no longer has a low battery";
        var body = $"The temperature sensor in {locationName} no longer has a low battery.";

        _logger.LogWarning("Alert clear: {Subject}", subject);

        await SendEmailAsync(subject, body, device, stoppingToken);
    }

    private async Task SendEmailAsync(string subject, string body, TemperatureDeviceResponse device, CancellationToken stoppingToken)
    {
        var deviceName = $"{device.Name} ({device.MqttTopic})";
        var readingTempString = TemperatureHelpers.GetDualTempString(device.LastReading?.TemperatureCelsius);
        var readingTime = device.LastReading?.Time;

        await _emailNotificationService.SendAsync(e =>
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
