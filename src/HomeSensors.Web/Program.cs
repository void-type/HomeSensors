using HomeSensors.Model.Startup;
using HomeSensors.Web.Auth;
using HomeSensors.Web.Hubs;
using HomeSensors.Web.Startup;
using Serilog;
using VoidCore.AspNet.Configuration;
using VoidCore.AspNet.Logging;
using VoidCore.AspNet.Routing;
using VoidCore.AspNet.Security;
using VoidCore.Model.Auth;
using VoidCore.Model.Configuration;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var env = builder.Environment;
    var config = builder.Configuration;
    var services = builder.Services;

    Log.Logger = new LoggerConfiguration()
        // Set a default logger if none configured or configuration not found.
        .WriteTo.Console()
        .ReadFrom.Configuration(config)
        .CreateLogger();

    builder.Host.UseSerilog();

    Log.Information("Configuring host for {Name} v{Version}", ThisAssembly.AssemblyTitle, ThisAssembly.AssemblyInformationalVersion);

    // Settings
    var settings = services.AddSettingsSingleton<WebApplicationSettings>(config, true);
    settings.Validate();
    services.AddSingleton<ApplicationSettings>(settings);

    // Infrastructure
    services.AddHttpContextAccessor();
    services.AddControllers();
    services.AddSpaSecurityServices(env);
    services.AddApiExceptionFilter();
    services.AddSwagger(env);
    services.AddSignalR();

    // Authorization
    services.AddSingleton<ICurrentUserAccessor, SingleUserAccessor>();

    // Dependencies
    services.AddHomeSensorsWebServices(config);
    services.AddHomeSensorsCommonServices(config);

    var app = builder.Build();

    // Middleware pipeline
    app.UseAlwaysOnShortCircuit();
    app.UseSpaExceptionPage(env);
    app.UseSecureTransport(env);
    app.UseSecurityHeaders(env, config);
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRequestLoggingScope();
    app.UseSerilogRequestLogging();
    app.UseCurrentUserLogging();
    app.UseSwaggerAndUi();
    app.MapHub<TemperaturesHub>("/hub/temperatures");
    app.UseSpaEndpoints();

    Log.Information("Starting host.");
    await app.RunAsync();
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
