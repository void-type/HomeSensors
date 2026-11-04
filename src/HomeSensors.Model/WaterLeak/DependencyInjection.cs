using HomeSensors.Model.WaterLeak.Repositories;
using HomeSensors.Model.WaterLeak.Services;
using HomeSensors.Model.WaterLeak.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoidCore.Model.Configuration;

namespace HomeSensors.Model.WaterLeak;

public static class DependencyInjection
{
    public static IServiceCollection AddWaterLeakServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<WaterLeakDeviceRepository>();

        var mqttWaterLeaksSettings = services.AddSettingsSingleton<MqttWaterLeaksSettings>(config.GetSection("Workers"));
        if (mqttWaterLeaksSettings.IsEnabled)
        {
            services.AddSingleton<WaterLeakAlertState>();
            services.AddScoped<WaterLeakAlertService>();
            services.AddHostedService<MqttWaterLeaksWorker>();
        }

        return services;
    }
}
