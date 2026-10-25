using HomeSensors.Model.Categories.Repositories;
using HomeSensors.Model.Notifications;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Services;

public class TemperatureHubNotifier(IHubContext<TemperaturesHub> hubContext, IServiceScopeFactory scopeFactory) : ITemperatureHubNotifier
{
    public async Task NotifyCurrentReadingsChangedAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var readingRepository = scope.ServiceProvider.GetRequiredService<TemperatureReadingRepository>();
        var currentReadings = await readingRepository.GetCurrentCachedAsync(cancellationToken, refreshCache: true);

        var categoryRepository = scope.ServiceProvider.GetRequiredService<CategoryRepository>();
        var categories = await categoryRepository.GetAllAsync(cancellationToken);

        await Task.WhenAll(
            hubContext.Clients.All.SendAsync(
                TemperaturesHub.UpdateCurrentReadingsMessageName,
                currentReadings,
                cancellationToken: cancellationToken),

            hubContext.Clients.All.SendAsync(
                TemperaturesHub.UpdateCategoriesMessageName,
                categories,
                cancellationToken: cancellationToken));
    }
}
