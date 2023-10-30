using HomeSensors.Model.Alerts;
using HomeSensors.Model.Data;
using HomeSensors.Model.Emailing;
using HomeSensors.Model.Mqtt;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Workers;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using Serilog;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
using VoidCore.Model.Time;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        services.AddSettingsSingleton<ApplicationSettings>(context.Configuration, true).Validate();
        services.AddSettingsSingleton<MqttSettings>(context.Configuration);
        services.AddSettingsSingleton<NotificationsSettings>(context.Configuration);
        var workersSettings = services.AddSettingsSingleton<WorkersSettings>(context.Configuration);

        services.AddDbContext<HomeSensorsContext>(ctxOptions => ctxOptions
            .UseSqlServer("Name=HomeSensors", sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(HomeSensorsContext).Assembly.FullName);
                sqlOptions.CommandTimeout(120);
            }));

        services.AddScoped<TemperatureReadingRepository>();
        services.AddScoped<TemperatureDeviceRepository>();
        services.AddScoped<TemperatureLocationRepository>();

        services.AddSingleton<IEmailFactory, HtmlEmailFactory>();
        services.AddSingleton<IEmailSender, SmtpEmailer>();
        services.AddSingleton<EmailNotificationService>();
        services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
        services.AddSingleton<MqttFactory>();

        services.AddScoped<AlertTemperatureLimitsService>();
        services.AddScoped<AlertDevicesService>();

        if (workersSettings.AlertsEnabled)
        {
            services.AddHostedService<AlertsWorker>();
        }

        if (workersSettings.MqttTemperaturesEnabled)
        {
            services.AddHostedService<MqttTemperaturesWorker>();
        }

        if (workersSettings.SummarizeTemperatureReadingsEnabled)
        {
            services.AddHostedService<SummarizeTemperatureReadingsWorker>();
        }
    })
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

try
{
    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    await host.RunAsync();

    Log.Information("Starting host.");
    host.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
    return 1;
}
finally
{
    Log.Information("Stopping host.");
    Log.CloseAndFlush();
}
