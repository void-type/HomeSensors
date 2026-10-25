using HomeSensors.Model;
using HomeSensors.Model.Data;
using HomeSensors.Web.Auth;
using HomeSensors.Web.Hubs;
using HomeSensors.Web.Startup;
using Microsoft.EntityFrameworkCore;
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

    builder.Host.UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

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
    services.AddHomeSensorsCommonServices(config);
    services.AddHomeSensorsWebServices(config);

    var app = builder.Build();

    // Apply pending migrations before hosted services (workers) start.
    // EF tooling aborts the host at Build(), so this doesn't run during dotnet ef commands.
    var contextFactory = app.Services.GetRequiredService<IDbContextFactory<HomeSensorsContext>>();
    await using (var context = await contextFactory.CreateDbContextAsync())
    {
        Log.Information("Applying database migrations.");
        await context.Database.MigrateAsync();
    }

    // Middleware pipeline
    app.UseAlwaysOnShortCircuit();
    app.UseSpaExceptionPage(env);
    app.UseSecureTransport(env);
    app.UseSecurityHeaders(env, config);
    app.MapStaticAssets();
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
