using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.HomeAssistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;

namespace HomeSensors.Model.Services.Thermostat;

public class ThermostatActionsWorker : BackgroundService
{
    private readonly ILogger<ThermostatActionsWorker> _logger;
    private readonly ThermostatActionsSettings _thermostatWorkerSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public ThermostatActionsWorker(
        ILogger<ThermostatActionsWorker> logger,
        ThermostatActionsSettings thermostatWorkerSettings,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _thermostatWorkerSettings = thermostatWorkerSettings;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(ThermostatActionsWorker),
            _thermostatWorkerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Home Assistant only goes back 10 days by default. We'll do 15 to be safe and catch anything we missed.
        var lastStateChange = DateTimeOffset.UtcNow.AddDays(-15);

        var betweenTicks = TimeSpan.FromMinutes(_thermostatWorkerSettings.BetweenTicksMinutes);
        var timer = new PeriodicTimer(betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(ThermostatActionsWorker));

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

                using var httpClient = _httpClientFactory.CreateClient("HomeAssistant");

                var entityId = _thermostatWorkerSettings.EntityId;
                var startTimestamp = Uri.EscapeDataString(lastStateChange.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                var endTimestamp = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                var apiUrl = $"/api/history/period/{startTimestamp}?end_time={endTimestamp}&filter_entity_id={Uri.EscapeDataString(entityId)}";

                var response = await httpClient.GetAsync(apiUrl, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var stateHistoryList = await response.Content
                        .ReadFromJsonAsync<List<List<EntityHistoryState>>>(cancellationToken: stoppingToken);

                    foreach (var stateEntry in stateHistoryList?.FirstOrDefault()?.OrderBy(state => state.LastChanged).ToArray() ?? [])
                    {
                        var match = await dbContext.ThermostatActions
                            .FirstOrDefaultAsync(
                                action => action.EntityId == stateEntry.EntityId && action.LastChanged == stateEntry.LastChanged,
                                stoppingToken);

                        if (match is not null)
                        {
                            continue;
                        }

                        dbContext.ThermostatActions.Add(new ThermostatAction
                        {
                            EntityId = stateEntry.EntityId,
                            State = stateEntry.State,
                            LastChanged = stateEntry.LastChanged,
                            LastUpdated = stateEntry.LastUpdated
                        });

                        var entityLastChanged = stateEntry.LastChanged ?? DateTimeOffset.MinValue;

                        if (lastStateChange < entityLastChanged)
                        {
                            lastStateChange = entityLastChanged;
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to retrieve state history. Status: {StatusCode}, Reason: {ReasonPhrase}",
                        response.StatusCode,
                        response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(ThermostatActionsWorker));
            }
            finally
            {
                _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(ThermostatActionsWorker), Stopwatch.GetElapsedTime(startTime));
            }
        }
    }
}
