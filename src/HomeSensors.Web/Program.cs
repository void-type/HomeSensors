using HomeSensors.Data;
using HomeSensors.Data.Repositories;
using HomeSensors.Web.Auth;
using HomeSensors.Web.Temperatures;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Configuration;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Auth;
using VoidCore.Model.Configuration;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
var env = builder.Environment;

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

try
{
    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    services.AddSettingsSingleton<WebApplicationSettings>(config, true).Validate();

    services.AddControllersWithViews();

    // Store migrations in the Data project
    services.AddDbContext<HomeSensorsContext>(options =>
    {
        options
            .UseSqlServer("Name=HomeSensors",
                b => b.MigrationsAssembly(typeof(HomeSensorsContext).Assembly.FullName));
    });

    services.AddScoped<TemperatureRepository>();

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

    services.AddSingleton<ICurrentUserAccessor, SingleUserAccessor>();

    // Auto-register Domain Events
    services.AddDomainEvents(
        ServiceLifetime.Scoped,
        typeof(GetWebClientInfo).Assembly);

    services.AddHostedService<CurrentReadingsWorker>();
    services.AddSignalR();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
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
