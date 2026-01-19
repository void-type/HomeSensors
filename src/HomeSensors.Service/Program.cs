using HomeSensors.Model;
using Serilog;
using VoidCore.Model.Configuration;

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        })
        .UseWindowsService()
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            var config = context.Configuration;

            services.AddSettingsSingleton<ApplicationSettings>(config, true).Validate();

            services.AddHomeSensorsCommonServices(config);
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
