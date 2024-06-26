﻿using HomeSensors.Model.Alerts;
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

            var workersConfig = config.GetSection("Workers");

            var alertsSettings = services.AddSettingsSingleton<AlertsSettings>(workersConfig);
            if (alertsSettings.IsEnabled)
            {
                Log.Information("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
                    nameof(AlertsWorker),
                    alertsSettings.BetweenTicksMinutes);
                services.AddHostedService<AlertsWorker>();
            }

            var mqttTemperaturesSettings = services.AddSettingsSingleton<MqttTemperaturesSettings>(workersConfig);
            if (mqttTemperaturesSettings.IsEnabled)
            {
                Log.Information("Enabling background job: {JobName}.",
                    nameof(MqttTemperaturesWorker));
                services.AddHostedService<MqttTemperaturesWorker>();
            }

            var summarizeSettings = services.AddSettingsSingleton<SummarizeTemperatureReadingsSettings>(workersConfig);
            if (summarizeSettings.IsEnabled)
            {
                Log.Information("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
                    nameof(SummarizeTemperatureReadingsSettings),
                    summarizeSettings.BetweenTicksMinutes);
                services.AddHostedService<SummarizeTemperatureReadingsWorker>();
            }

            var mqttWaterLeaksSettings = services.AddSettingsSingleton<MqttWaterLeaksSettings>(workersConfig);
            if (mqttWaterLeaksSettings.IsEnabled)
            {
                Log.Information("Enabling background job: {JobName}.",
                    nameof(MqttWaterLeaksSettings));
                services.AddHostedService<MqttWaterLeaksWorker>();
            }
        })
        .Build();

    var config = host.Services.GetRequiredService<IConfiguration>();

    Log.Logger = new LoggerConfiguration()
        // Set a default logger if none configured or configuration not found.
        .WriteTo.Console()
        .ReadFrom.Configuration(config)
        .CreateLogger();

    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    await host.RunAsync();

    Log.Information("Starting host.");
    await host.RunAsync();
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
    await Log.CloseAndFlushAsync();
}
