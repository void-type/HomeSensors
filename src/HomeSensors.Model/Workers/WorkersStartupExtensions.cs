using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoidCore.Model.Configuration;

namespace HomeSensors.Model.Workers;

public static class WorkersStartupExtensions
{
    public static IServiceCollection AddWorkersService(this IServiceCollection services, IConfiguration config)
    {
        var workersConfig = config.GetSection("Workers");

        var alertsSettings = services.AddSettingsSingleton<AlertsSettings>(workersConfig);
        if (alertsSettings.IsEnabled)
        {
            services.AddHostedService<AlertsWorker>();
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

        var mqttWaterLeaksSettings = services.AddSettingsSingleton<MqttWaterLeaksSettings>(workersConfig);
        if (mqttWaterLeaksSettings.IsEnabled)
        {
            services.AddHostedService<MqttWaterLeaksWorker>();
        }

        return services;
    }
}
