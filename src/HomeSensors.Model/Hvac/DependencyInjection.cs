using HomeSensors.Model.Hvac.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoidCore.Model.Configuration;

namespace HomeSensors.Model.Hvac;

public static class DependencyInjection
{
    public static IServiceCollection AddHvacServices(this IServiceCollection services, IConfiguration config)
    {
        var hvacSettings = services.AddSettingsSingleton<HvacActionsSettings>(config.GetSection("Workers"));
        if (hvacSettings.IsEnabled)
        {
            services.AddHostedService<HvacActionsWorker>();
        }

        return services;
    }
}
