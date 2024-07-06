using HomeSensors.Web.Workers;
using VoidCore.Model.Configuration;

namespace HomeSensors.Web.Startup;

public static class WorkersWebStartupExtensions
{
    public static IServiceCollection AddWorkersWeb(this IServiceCollection services, IConfiguration config)
    {
        var workersConfig = config.GetSection("Workers");

        var pushTempsSettings = services.AddSettingsSingleton<PushTemperatureCurrentReadingsSettings>(workersConfig);
        if (pushTempsSettings.IsEnabled)
        {
            services.AddHostedService<PushTemperatureCurrentReadingsWorker>();
        }

        return services;
    }
}
