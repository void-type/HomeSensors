using HomeSensors.Data.Repositories;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Service.Workers;

public class SendCurrentReadingsToClients : BackgroundService
{
    private readonly IHubContext<TemperatureHub> _tempHubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public SendCurrentReadingsToClients(IHubContext<TemperatureHub> tempHubContext, IServiceScopeFactory scopeFactory)
    {
        _tempHubContext = tempHubContext;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var temperatureRepository = scope.ServiceProvider.GetRequiredService<TemperatureRepository>();

            var currentReadings = await temperatureRepository.GetCurrentReadings();
            await _tempHubContext.Clients.All.SendAsync("updateCurrentReadings", currentReadings, cancellationToken: stoppingToken);
        }
    }
}
