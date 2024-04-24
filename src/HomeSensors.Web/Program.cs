using HomeSensors.Model.Caching;
using HomeSensors.Model.Data;
using HomeSensors.Model.Mqtt;
using HomeSensors.Model.Repositories;
using HomeSensors.Web.Auth;
using HomeSensors.Web.Hubs;
using HomeSensors.Web.Repositories;
using HomeSensors.Web.Services;
using HomeSensors.Web.Startup;
using HomeSensors.Web.Workers;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using Serilog;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Configuration;
using VoidCore.AspNet.Logging;
using VoidCore.AspNet.Routing;
using VoidCore.AspNet.Security;
using VoidCore.Model.Auth;
using VoidCore.Model.Configuration;
using VoidCore.Model.Time;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;
    var services = builder.Services;
    var env = builder.Environment;

    Log.Logger = new LoggerConfiguration()
        // Set a default logger if none configured or configuration not found.
        .WriteTo.Console()
        .ReadFrom.Configuration(config)
        .CreateLogger();

    builder.Host.UseSerilog();

    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    // Settings
    services.AddSettingsSingleton<WebApplicationSettings>(config, true).Validate();
    services.AddSettingsSingleton<MqttSettings>(config);
    services.AddSettingsSingleton<CachingSettings>(config);

    // Infrastructure
    services.AddControllers();
    services.AddSpaSecurityServices(env);
    services.AddApiExceptionFilter();

    // Authorization

    // Dependencies
    services.AddHttpContextAccessor();
    services.AddSingleton<ICurrentUserAccessor, SingleUserAccessor>();
    services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
    services.AddSingleton<MqttFeedDiscoveryService>();
    services.AddSingleton<MqttFactory>();

    config.GetRequiredConnectionString<HomeSensorsContext>();
    services.AddDbContext<HomeSensorsContext>(options => options
        .UseSqlServer("Name=HomeSensors", b => b.MigrationsAssembly(typeof(HomeSensorsContext).Assembly.FullName)));

    services.AddScoped<TemperatureReadingRepository>();
    services.AddScoped<TemperatureDeviceRepository>();
    services.AddScoped<TemperatureLocationRepository>();

    services.AddLazyCache(sp =>
    {
        var cachingSettings = sp.GetRequiredService<CachingSettings>();
        var cache = new CachingService(CachingService.DefaultCacheProvider);
        cache.DefaultCachePolicy.DefaultCacheDurationSeconds = cachingSettings.DefaultMinutes * 60;
        return cache;
    });

    services.AddScoped<TemperatureCachedRepository>();

    services.AddDomainEvents(
        ServiceLifetime.Scoped,
        typeof(GetWebClientInfo).Assembly);

    services.AddSignalR();

    services.AddSwaggerWithCsp();

    var workersConfig = config.GetSection("Workers");

    var pushTempsSettings = services.AddSettingsSingleton<PushTemperatureCurrentReadingsSettings>(workersConfig);
    if (pushTempsSettings.IsEnabled)
    {
        services.AddHostedService<PushTemperatureCurrentReadingsWorker>();
    }

    var app = builder.Build();

    app.UseAlwaysOnShortCircuit();
    app.UseSpaExceptionPage(env);
    app.UseSecureTransport(env);
    app.UseSecurityHeaders(env);
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRequestLoggingScope();
    app.UseSerilogRequestLogging();
    app.UseCurrentUserLogging();
    app.UseSwaggerAndUi(env);
    app.MapHub<TemperaturesHub>("/hub/temperatures");
    app.UseSpaEndpoints();

    Log.Information("Starting host.");
    app.Run();
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
