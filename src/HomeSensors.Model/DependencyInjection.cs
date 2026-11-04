using HomeSensors.Model.Cameras;
using HomeSensors.Model.Categories.Repositories;
using HomeSensors.Model.Data;
using HomeSensors.Model.Hvac;
using HomeSensors.Model.Infrastructure.Emailing;
using HomeSensors.Model.Infrastructure.Emailing.Repositories;
using HomeSensors.Model.Infrastructure.HomeAssistant;
using HomeSensors.Model.Infrastructure.Mqtt;
using HomeSensors.Model.Temperature;
using HomeSensors.Model.WaterLeak;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using System.Net.Http.Headers;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
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
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<MqttFactory>();

        services.AddScoped<CategoryRepository>();
        services.AddScoped<EmailRecipientRepository>();

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
                sqlOptions.EnableRetryOnFailure();
            }));

        services.AddCameraServices(config);
        services.AddTemperatureServices(config);
        services.AddWaterLeakServices(config);
        services.AddHvacServices(config);

        return services;
    }
}
