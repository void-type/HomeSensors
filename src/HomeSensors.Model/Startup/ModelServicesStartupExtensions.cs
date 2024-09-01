using HomeSensors.Model.Cache;
using HomeSensors.Model.Cache.Configuration;
using HomeSensors.Model.Data;
using HomeSensors.Model.Emailing;
using HomeSensors.Model.Mqtt;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Services.Temperature.Alert;
using HomeSensors.Model.Services.Temperature.Poll;
using HomeSensors.Model.Services.Temperature.Summarize;
using HomeSensors.Model.Services.WaterLeak;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Startup;

public static class ModelServicesStartupExtensions
{
    public static IServiceCollection AddHomeSensorsCommonServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSettingsSingleton<MqttSettings>(config);
        services.AddSettingsSingleton<NotificationsSettings>(config);
        services.AddSettingsSingleton<CacheSettings>(config);

        services.AddSingleton<IEmailFactory, HtmlEmailFactory>();
        services.AddSingleton<IEmailSender, SmtpEmailer>();
        services.AddSingleton<EmailNotificationService>();
        services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
        services.AddSingleton<MqttFactory>();
        services.AddSingleton<LazyCacheOptionService>();

        services.AddScoped<TemperatureReadingRepository>();
        services.AddScoped<TemperatureDeviceRepository>();
        services.AddScoped<TemperatureLocationRepository>();

        services.AddLazyCache(sp =>
        {
            var cacheOptions = sp.GetRequiredService<LazyCacheOptionService>();
            var cache = new LazyCache.CachingService(LazyCache.CachingService.DefaultCacheProvider);
            // If not using our lazy option overrides, the fallback will be sliding expiration (we cannot set a global mode in LazyCache) and our default/fallback duration.
            cache.DefaultCachePolicy.DefaultCacheDurationSeconds = (int)double.Round(cacheOptions.GetPolicyFromSettings().Duration.TotalSeconds);
            return cache;
        });

        config.GetRequiredConnectionString<HomeSensorsContext>();
        services.AddDbContext<HomeSensorsContext>(ctxOptions => ctxOptions
            .UseSqlServer("Name=HomeSensors", sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(HomeSensorsContext).Assembly.FullName);
                sqlOptions.CommandTimeout(120);
            }));

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

        var mqttWaterLeaksSettings = services.AddSettingsSingleton<MqttWaterLeaksSettings>(workersConfig);
        if (mqttWaterLeaksSettings.IsEnabled)
        {
            services.AddSingleton<WaterLeakAlertService>();
            services.AddHostedService<MqttWaterLeaksWorker>();
        }

        return services;
    }
}
