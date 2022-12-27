using HomeSensors.Web.Hubs;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Workers;

/// <summary>
/// This worker broadcasts current readings to all of the TemperatureHub SignalR clients.
/// </summary>
public class PushTemperatureCurrentReadingsWorker : BackgroundService
{
    private const string MessageName = "updateCurrentReadings";

    private readonly TimeSpan _betweenTicks = TimeSpan.FromSeconds(30);
    private readonly IHubContext<TemperatureHub> _tempHubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public PushTemperatureCurrentReadingsWorker(IHubContext<TemperatureHub> tempHubContext, IServiceScopeFactory scopeFactory)
    {
        _tempHubContext = tempHubContext;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var temperatureRepository = scope.ServiceProvider.GetRequiredService<TemperatureCachedRepository>();

            var currentReadings = await temperatureRepository.GetCurrentReadings(true);

            await _tempHubContext.Clients.All.SendAsync(MessageName, currentReadings, cancellationToken: stoppingToken);
        }
    }
}
