using HomeSensors.Model.Categories.Repositories;
using HomeSensors.Model.Data;
using HomeSensors.Model.Hvac.Workers;
using HomeSensors.Model.Infrastructure.Emailing;
using HomeSensors.Model.Infrastructure.Emailing.Repositories;
using HomeSensors.Model.Infrastructure.HomeAssistant;
using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Model.Notifications;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Model.Temperature.Services;
using HomeSensors.Model.Temperature.Workers;
using HomeSensors.Model.WaterLeak.Repositories;
using HomeSensors.Model.WaterLeak.Services;
using HomeSensors.Model.WaterLeak.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using System.Net.Http.Headers;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
using VoidCore.Model.Time;
using ZiggyCreatures.Caching.Fusion;

namespace HomeSensors.Model;

public static class DependencyInjection
{
    public static IServiceCollection AddHomeSensorsCommonServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSettingsSingleton<MqttSettings>(config);
        services.AddSettingsSingleton<NotificationsSettings>(config);
        services.AddSettingsSingleton<HomeAssistantSettings>(config);

        services.AddSingleton<IEmailFactory, HtmlEmailFactory>();
        services.AddSingleton<IEmailSender, SmtpEmailer>();
        services.AddScoped<EmailNotificationService>();
        services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
        services.AddSingleton<MqttFactory>();

        services.AddScoped<CategoryRepository>();
        services.AddScoped<TemperatureReadingRepository>();
        services.AddScoped<TemperatureDeviceRepository>();
        services.AddScoped<TemperatureLocationRepository>();
        services.AddScoped<WaterLeakDeviceRepository>();
        services.AddScoped<EmailRecipientRepository>();

        services.AddSingleton<ITemperatureHubNotifier, NoOpTemperatureHubNotifier>();

        services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5),
            })
            .AsHybridCache();

        services.AddHttpClient("HomeAssistant", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<HomeAssistantSettings>();
            client.BaseAddress = new Uri(settings.ApiEndpoint);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        });

        config.GetRequiredConnectionString<HomeSensorsContext>();
        services.AddDbContextFactory<HomeSensorsContext>(ctxOptions => ctxOptions
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
            services.AddSingleton<WaterLeakAlertState>();
            services.AddScoped<WaterLeakAlertService>();
            services.AddHostedService<MqttWaterLeaksWorker>();
        }

        var hvacSettings = services.AddSettingsSingleton<HvacActionsSettings>(workersConfig);
        if (hvacSettings.IsEnabled)
        {
            services.AddHostedService<HvacActionsWorker>();
        }

        return services;
    }
}
