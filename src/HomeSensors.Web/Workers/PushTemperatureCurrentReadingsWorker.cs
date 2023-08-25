using HomeSensors.Model.Workers;
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

    private readonly TimeSpan _betweenTicks;
    private readonly ILogger<PushTemperatureCurrentReadingsWorker> _logger;
    private readonly IHubContext<TemperatureHub> _tempHubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public PushTemperatureCurrentReadingsWorker(ILogger<PushTemperatureCurrentReadingsWorker> logger,
        IHubContext<TemperatureHub> tempHubContext, IServiceScopeFactory scopeFactory,
        WorkersSettings workersSettings)
    {
        _logger = logger;
        _tempHubContext = tempHubContext;
        _scopeFactory = scopeFactory;
        _betweenTicks = TimeSpan.FromSeconds(workersSettings.PushTemperatureCurrentReadingsBetweenTicksSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var temperatureRepository = scope.ServiceProvider.GetRequiredService<TemperatureCachedRepository>();

                var currentReadings = await temperatureRepository.GetCurrentReadings(true);

                await _tempHubContext.Clients.All.SendAsync(MessageName, currentReadings, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(PushTemperatureCurrentReadingsWorker));
            }
        }
    }
}
