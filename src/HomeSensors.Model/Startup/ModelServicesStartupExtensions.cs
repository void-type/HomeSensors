using HomeSensors.Model.Data;
using HomeSensors.Model.Emailing;
using HomeSensors.Model.Mqtt;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Services.Temperature.Alert;
using HomeSensors.Model.Services.Temperature.Poll;
using HomeSensors.Model.Services.Temperature.Summarize;
using HomeSensors.Model.Services.WaterLeak;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
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

        services.AddSingleton<IEmailFactory, HtmlEmailFactory>();
        services.AddSingleton<IEmailSender, SmtpEmailer>();
        services.AddSingleton<EmailNotificationService>();
        services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
        services.AddSingleton<MqttFactory>();

        services.AddScoped<CategoryRepository>();
        services.AddScoped<TemperatureReadingRepository>();
        services.AddScoped<TemperatureDeviceRepository>();
        services.AddScoped<TemperatureLocationRepository>();

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        services.AddHybridCache(options =>
        {
            // X * 1MB
            options.MaximumPayloadBytes = 100 * 1048576L;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
            };
        });
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
