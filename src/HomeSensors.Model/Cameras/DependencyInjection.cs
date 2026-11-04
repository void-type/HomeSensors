using HomeSensors.Model.Cameras.Repositories;
using HomeSensors.Model.Cameras.Services;
using HomeSensors.Model.Cameras.Settings;
using HomeSensors.Model.Cameras.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoidCore.Model.Configuration;

namespace HomeSensors.Model.Cameras;

public static class DependencyInjection
{
    public static IServiceCollection AddCameraServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSettingsSingleton<CameraSettings>(config.GetSection("CameraSnapshots"), root: true);
        services.AddScoped<CameraRepository>();
        services.AddScoped<ThumbnailService>();
        services.AddScoped<CameraSnapshotRepository>();

        var cameraWorkerSettings = services.AddSettingsSingleton<CameraWorkerSettings>(config.GetSection("CameraSnapshots:Worker"), root: true);
        if (cameraWorkerSettings.IsEnabled)
        {
            services.AddHostedService<CameraSnapshotThumbnailWorker>();
        }

        return services;
    }
}
