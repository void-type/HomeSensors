using HomeSensors.Model.Categories.Repositories;
using HomeSensors.Model.Notifications;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Services;

public class TemperatureHubNotifier : ITemperatureHubNotifier
{
    private readonly IHubContext<TemperaturesHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public TemperatureHubNotifier(IHubContext<TemperaturesHub> hubContext, IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task NotifyCurrentReadingsChangedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();

        var readingRepository = scope.ServiceProvider.GetRequiredService<TemperatureReadingRepository>();
        var currentReadings = await readingRepository.GetCurrentCachedAsync(true, cancellationToken);

        var categoryRepository = scope.ServiceProvider.GetRequiredService<CategoryRepository>();
        var categories = await categoryRepository.GetAllAsync();

        await Task.WhenAll(
            _hubContext.Clients.All.SendAsync(
                TemperaturesHub.UpdateCurrentReadingsMessageName,
                currentReadings,
                cancellationToken: cancellationToken),

            _hubContext.Clients.All.SendAsync(
                TemperaturesHub.UpdateCategoriesMessageName,
                categories,
                cancellationToken: cancellationToken));
    }
}
