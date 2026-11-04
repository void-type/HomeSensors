using HomeSensors.Model.Notifications;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Model.Temperature.Services;
using HomeSensors.Model.Temperature.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoidCore.Model.Configuration;

namespace HomeSensors.Model.Temperature;

public static class DependencyInjection
{
    public static IServiceCollection AddTemperatureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<TemperatureReadingRepository>();
        services.AddScoped<TemperatureDeviceRepository>();
        services.AddScoped<TemperatureLocationRepository>();

        services.AddSingleton<ITemperatureHubNotifier, NoOpTemperatureHubNotifier>();

        var workersConfig = config.GetSection("Workers");

        var alertsSettings = services.AddSettingsSingleton<TemperatureAlertsSettings>(workersConfig);
        if (alertsSettings.IsEnabled)
        {
            services.AddScoped<TemperatureLimitAlertService>();
            services.AddScoped<TemperatureDeviceAlertService>();
            services.AddHostedService<TemperatureAlertsWorker>();
        }

        var mqttTemperaturesSettings = services.AddSettingsSingleton<MqttTemperaturesSettings>(workersConfig);
        if (mqttTemperaturesSettings.IsEnabled)
        {
            services.AddHostedService<MqttTemperaturesWorker>();
        }

        var summarizeSettings = services.AddSettingsSingleton<SummarizeTemperatureReadingsSettings>(workersConfig);
        if (summarizeSettings.IsEnabled)
        {
            services.AddHostedService<SummarizeTemperatureReadingsWorker>();
        }

        return services;
    }
}
