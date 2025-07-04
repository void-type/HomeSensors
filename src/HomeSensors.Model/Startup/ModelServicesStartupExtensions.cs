using HomeSensors.Model.Data;
using HomeSensors.Model.Emailing;
using HomeSensors.Model.HomeAssistant;
using HomeSensors.Model.Mqtt;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Services.Temperature.Alert;
using HomeSensors.Model.Services.Temperature.Poll;
using HomeSensors.Model.Services.Temperature.Summarize;
using HomeSensors.Model.Services.Thermostat;
using HomeSensors.Model.Services.WaterLeak;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using System.Net.Http.Headers;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
using VoidCore.Model.Time;
using ZiggyCreatures.Caching.Fusion;

namespace HomeSensors.Model.Startup;

public static class ModelServicesStartupExtensions
{
    public static IServiceCollection AddHomeSensorsCommonServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSettingsSingleton<MqttSettings>(config);
        services.AddSettingsSingleton<NotificationsSettings>(config);
        services.AddSettingsSingleton<HomeAssistantSettings>(config);

        services.AddSingleton<IEmailFactory, HtmlEmailFactory>();
        services.AddSingleton<IEmailSender, SmtpEmailer>();
        services.AddSingleton<EmailNotificationService>();
        services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
        services.AddSingleton<MqttFactory>();

        services.AddScoped<CategoryRepository>();
        services.AddScoped<TemperatureReadingRepository>();
        services.AddScoped<TemperatureDeviceRepository>();
        services.AddScoped<TemperatureLocationRepository>();

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

        var thermostatSettings = services.AddSettingsSingleton<ThermostatActionsSettings>(workersConfig);
        if (thermostatSettings.IsEnabled)
        {
            services.AddHostedService<ThermostatActionsWorker>();
        }

        return services;
    }
}
