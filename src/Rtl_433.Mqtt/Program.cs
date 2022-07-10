using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

using Rtl_433.Mqtt;


IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        var mqttClient = context.Configuration.GetSection("Mqtt").Get<MqttConfiguration>();
        services.AddSingleton(mqttClient);
        services.AddHostedService<Worker>();
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
