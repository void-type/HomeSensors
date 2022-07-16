using HomeSensors.Data;
using HomeSensors.Service.Models;
using HomeSensors.Service.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        var mqttClient = context.Configuration.GetSection("Mqtt").Get<MqttConfiguration>();
        services.AddSingleton(mqttClient);
        services.AddDbContext<HomeSensorsContext>(options => options
            .UseSqlServer("Name=HomeSensors", b => b.MigrationsAssembly(typeof(HomeSensorsContext).Assembly.FullName)));
        services.AddHostedService<Get433MhzTemperaturesWorker>();
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
