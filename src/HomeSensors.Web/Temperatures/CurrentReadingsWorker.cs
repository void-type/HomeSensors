using HomeSensors.Data.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Temperatures;

/// <summary>
/// This worker broadcasts all of the TemperatureHub SignalR clients with current readings.
/// </summary>
public class CurrentReadingsWorker : BackgroundService
{
    private const string MessageName = "updateCurrentReadings";

    private readonly TimeSpan _betweenTicks = TimeSpan.FromSeconds(30);
    private readonly IHubContext<TemperatureHub> _tempHubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public CurrentReadingsWorker(IHubContext<TemperatureHub> tempHubContext, IServiceScopeFactory scopeFactory)
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
            var temperatureRepository = scope.ServiceProvider.GetRequiredService<TemperatureRepository>();

            var currentReadings = await temperatureRepository.GetCurrentReadings();

            await _tempHubContext.Clients.All.SendAsync(MessageName, currentReadings, cancellationToken: stoppingToken);
        }
    }
}
