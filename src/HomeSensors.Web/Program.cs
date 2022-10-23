using HomeSensors.Data;
using HomeSensors.Data.Repositories;
using HomeSensors.Web.Auth;
using HomeSensors.Web.Caching;
using HomeSensors.Web.Temperatures;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Configuration;
using VoidCore.AspNet.Routing;
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

    services.AddSettingsSingleton<WebApplicationSettings>(config, true).Validate();
    services.AddSettingsSingleton<CachingSettings>(config);

    services.AddControllersWithViews();
    services.AddApiExceptionFilter();
    services.AddHttpContextAccessor();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        // Set the comments path for the Swagger JSON and UI.
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    services.AddDbContext<HomeSensorsContext>(options => options
        .UseSqlServer("Name=HomeSensors", b => b.MigrationsAssembly(typeof(HomeSensorsContext).Assembly.FullName)));

    services.AddScoped<TemperatureReadingRepository>();
    services.AddScoped<TemperatureDeviceRepository>();
    services.AddScoped<TemperatureLocationRepository>();

    services.AddLazyCache();
    services.AddScoped<CachedTemperatureRepository>();

    services.AddSingleton<IDateTimeService, UtcNowDateTimeService>();
    services.AddSingleton<ICurrentUserAccessor, SingleUserAccessor>();

    services.AddDomainEvents(ServiceLifetime.Scoped, typeof(GetWebClientInfo).Assembly);

    services.AddSignalR();
    services.AddHostedService<CurrentReadingsWorker>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSerilogRequestLogging();
    app.UseAuthorization();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = env.ApplicationName + " API");
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
