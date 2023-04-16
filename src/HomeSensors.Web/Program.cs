using HomeSensors.Model.Caching;
using HomeSensors.Model.Data;
using HomeSensors.Model.Repositories;
using HomeSensors.Web.Auth;
using HomeSensors.Web.Configuration;
using HomeSensors.Web.Hubs;
using HomeSensors.Web.Repositories;
using HomeSensors.Web.Workers;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Configuration;
using VoidCore.AspNet.Logging;
using VoidCore.AspNet.Routing;
using VoidCore.AspNet.Security;
using VoidCore.Model.Auth;
using VoidCore.Model.Configuration;
using VoidCore.Model.Time;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
var env = builder.Environment;

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    // Settings
    services.AddSettingsSingleton<WebApplicationSettings>(config, true).Validate();
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
    services.AddHostedService<PushTemperatureCurrentReadingsWorker>();

    services.AddSwaggerWithCsp();

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
    app.MapHub<TemperatureHub>("/hub/temperatures");
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
