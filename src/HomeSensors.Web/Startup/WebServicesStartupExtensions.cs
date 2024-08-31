using HomeSensors.Web.Services.MqttDiscovery;
using HomeSensors.Web.Services.PushTemperatureCurrentReadings;
using VoidCore.AspNet.ClientApp;
using VoidCore.Model.Configuration;

namespace HomeSensors.Web.Startup;

public static class WebServicesStartupExtensions
{
    public static IServiceCollection AddHomeSensorsWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<MqttDiscoveryService>();

        services.AddDomainEvents(
            ServiceLifetime.Scoped,
            typeof(GetWebClientInfo).Assembly);

        var workersConfig = config.GetSection("Workers");

        var pushTempsSettings = services.AddSettingsSingleton<PushTemperatureCurrentReadingsSettings>(workersConfig);
        if (pushTempsSettings.IsEnabled)
        {
            services.AddHostedService<PushTemperatureCurrentReadingsWorker>();
        }

        return services;
    }
}
