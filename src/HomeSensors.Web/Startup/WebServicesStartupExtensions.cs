using HomeSensors.Model.Notifications;
using HomeSensors.Web.Services;
using HomeSensors.Web.Services.MqttDiscovery;
using HomeSensors.Web.Services.PushTemperatureCurrentReadings;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VoidCore.AspNet.ClientApp;
using VoidCore.Model.Configuration;

namespace HomeSensors.Web.Startup;

public static class WebServicesStartupExtensions
{
    public static IServiceCollection AddHomeSensorsWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.Replace(ServiceDescriptor.Singleton<ITemperatureHubNotifier, TemperatureHubNotifier>());
        services.AddSingleton<MqttDiscoveryService>();

        services.AddDomainEvents(
            ServiceLifetime.Scoped,
            typeof(GetWebClientInfo).Assembly);

        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new FeatureFolderViewLocationExpander());
        });

        var workersConfig = config.GetSection("Workers");

        var pushTempsSettings = services.AddSettingsSingleton<PushTemperatureCurrentReadingsSettings>(workersConfig);
        if (pushTempsSettings.IsEnabled)
        {
            services.AddHostedService<PushTemperatureCurrentReadingsWorker>();
        }

        return services;
    }
}
