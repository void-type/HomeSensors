using HomeSensors.Model.Caching;
using HomeSensors.Web.Repositories;
using HomeSensors.Web.Services.MqttDiscovery;
using HomeSensors.Web.Services.PushTemperatureCurrentReadings;
using LazyCache;
using VoidCore.AspNet.ClientApp;
using VoidCore.Model.Configuration;

namespace HomeSensors.Web.Startup;

public static class WebServicesStartupExtensions
{
    public static IServiceCollection AddHomeSensorsWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<MqttDiscoveryService>();

        services.AddSettingsSingleton<CachingSettings>(config);

        services.AddLazyCache(sp =>
        {
            var cachingSettings = sp.GetRequiredService<CachingSettings>();
            var cache = new CachingService(CachingService.DefaultCacheProvider);
            cache.DefaultCachePolicy.DefaultCacheDurationSeconds = cachingSettings.DefaultMinutes * 60;
            return cache;
        });

        services.AddScoped<TemperatureCachedRepository>();

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
