using HomeSensors.Model.Data;
using HomeSensors.Web.Health;

namespace HomeSensors.Web.Startup;

public static class HealthStartupExtensions
{
    public static IServiceCollection AddHomeSensorsHealthChecks(this IServiceCollection services, IConfiguration config)
    {
        // The section is optional so existing deployments get the defaults.
        var settings = config.GetSection("HealthChecks").Get<HealthChecksSettings>() ?? new();
        services.AddSingleton(settings);

        var builder = services.AddHealthChecks()
            .AddDbContextCheck<HomeSensorsContext>("database");

        if (settings.IsReadingsCheckEnabled)
        {
            builder.AddCheck<TemperatureReadingsHealthCheck>("temperature-readings");
        }

        return services;
    }
}
