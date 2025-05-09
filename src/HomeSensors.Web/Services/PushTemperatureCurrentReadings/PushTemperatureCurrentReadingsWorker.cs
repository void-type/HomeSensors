﻿using HomeSensors.Model.Repositories;
using HomeSensors.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Services.PushTemperatureCurrentReadings;

/// <summary>
/// This worker broadcasts current readings to all of the TemperatureHub SignalR clients.
/// </summary>
public class PushTemperatureCurrentReadingsWorker : BackgroundService
{
    private readonly TimeSpan _betweenTicks;
    private readonly ILogger<PushTemperatureCurrentReadingsWorker> _logger;
    private readonly IHubContext<TemperaturesHub> _tempHubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public PushTemperatureCurrentReadingsWorker(ILogger<PushTemperatureCurrentReadingsWorker> logger,
        IHubContext<TemperaturesHub> tempHubContext, IServiceScopeFactory scopeFactory,
        PushTemperatureCurrentReadingsSettings workerSettings)
    {
        _logger = logger;
        _tempHubContext = tempHubContext;
        _scopeFactory = scopeFactory;
        _betweenTicks = TimeSpan.FromSeconds(workerSettings.BetweenTicksSeconds);

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} seconds.",
            nameof(PushTemperatureCurrentReadingsWorker),
            workerSettings.BetweenTicksSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var readingRepository = scope.ServiceProvider.GetRequiredService<TemperatureReadingRepository>();

                var currentReadings = await readingRepository.GetCurrentCachedAsync(true, stoppingToken);

                await _tempHubContext.Clients.All.SendAsync(TemperaturesHub.UpdateCurrentReadingsMessageName, currentReadings, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(PushTemperatureCurrentReadingsWorker));
            }
        }
    }
}
