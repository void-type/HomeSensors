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

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            var config = context.Configuration;

            services.AddSettingsSingleton<ApplicationSettings>(config, true).Validate();
            services.AddSettingsSingleton<MqttSettings>(config);
            services.AddSettingsSingleton<NotificationsSettings>(config);

            config.GetRequiredConnectionString<HomeSensorsContext>();

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

            services.AddScoped<TemperatureLimitAlertService>();
            services.AddScoped<DeviceAlertService>();
            services.AddSingleton<WaterLeakAlertService>();

            services.AddWorkersService(config);
        })
        .Build();

    var config = host.Services.GetRequiredService<IConfiguration>();

    Log.Logger = new LoggerConfiguration()
        // Set a default logger if none configured or configuration not found.
        .WriteTo.Console()
        .ReadFrom.Configuration(config)
        .CreateLogger();

    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    Log.Information("Starting host.");
    await host.RunAsync();
    return 0;
}
catch (HostAbortedException)
{
    // For EF tooling, let the exception throw and the tooling will catch it.
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
    return 1;
}
finally
{
    Log.Information("Stopping host.");
    await Log.CloseAndFlushAsync();
}
