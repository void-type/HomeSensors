using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.HomeAssistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Services.Thermostat;

public class ThermostatActionsWorker : BackgroundService
{
    private readonly ILogger<ThermostatActionsWorker> _logger;
    private readonly ThermostatActionsSettings _thermostatWorkerSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ThermostatActionsWorker(
        ILogger<ThermostatActionsWorker> logger,
        ThermostatActionsSettings thermostatWorkerSettings,
        IServiceScopeFactory scopeFactory,
        IDateTimeService dateTimeService,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _thermostatWorkerSettings = thermostatWorkerSettings;
        _scopeFactory = scopeFactory;
        _dateTimeService = dateTimeService;
        _httpClientFactory = httpClientFactory;

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(ThermostatActionsWorker),
            _thermostatWorkerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Home Assistant only goes back 10 days by default. We'll do 15 to be safe and catch anything we missed.
        var lastStateChanged = _dateTimeService.MomentWithOffset.AddDays(-15);
        var lastStateUpdated = _dateTimeService.MomentWithOffset.AddDays(-15);
        var lastState = "unknown";

        var entityId = _thermostatWorkerSettings.EntityId;

        using (var scope = _scopeFactory.CreateScope())
        {
            await using var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

            var lastEntry = await dbContext.ThermostatActions
                .Where(a => a.EntityId == entityId)
                .OrderByDescending(a => a.LastChanged)
                .ThenByDescending(a => a.LastUpdated)
                .FirstOrDefaultAsync(stoppingToken);

            if (lastEntry is not null)
            {
                lastStateChanged = lastEntry.LastChanged;
                lastStateUpdated = lastEntry.LastUpdated;
                lastState = lastEntry.State;
            }
        }

        var betweenTicks = TimeSpan.FromMinutes(_thermostatWorkerSettings.BetweenTicksMinutes);
        var timer = new PeriodicTimer(betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(ThermostatActionsWorker));

                using var scope = _scopeFactory.CreateScope();
                await using var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

                using var httpClient = _httpClientFactory.CreateClient("HomeAssistant");

                var startTimestamp = Uri.EscapeDataString(lastStateChanged.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                var endTimestamp = Uri.EscapeDataString(_dateTimeService.MomentWithOffset.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ"));
                var apiUrl = $"/api/history/period/{startTimestamp}?end_time={endTimestamp}&filter_entity_id={Uri.EscapeDataString(entityId)}";

                var response = await httpClient.GetAsync(apiUrl, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var stateHistoryList = await response.Content
                        .ReadFromJsonAsync<List<List<EntityHistoryState>>>(cancellationToken: stoppingToken);

                    var orderedStateEntries = stateHistoryList?
                        .FirstOrDefault()?
                        // Get where last updated greater than our last recorded change.
                        .Where(state => state.LastUpdated > lastStateUpdated)?
                        .OrderBy(state => state.LastChanged)?
                        .ThenBy(state => state.LastUpdated)?
                        .ToArray()
                        ?? [];

                    foreach (var stateEntry in orderedStateEntries)
                    {
                        // Store the last changed and last updated timestamps
                        if (stateEntry.LastChanged > lastStateChanged)
                        {
                            lastStateChanged = stateEntry.LastChanged ?? lastStateChanged;
                        }

                        if (stateEntry.LastUpdated > lastStateUpdated)
                        {
                            lastStateUpdated = stateEntry.LastUpdated ?? lastStateUpdated;
                        }

                        var state = stateEntry.State;

                        // If it's a thermostat, we want the hvac_action attribute
                        if (entityId.StartsWith("climate."))
                        {
                            var hvacActionElement = stateEntry.Attributes
                                .GetValueOrDefault("hvac_action", new JsonElement());

                            state = hvacActionElement.ValueKind == JsonValueKind.String
                               ? hvacActionElement.GetString()
                               : null;

                            if (string.IsNullOrWhiteSpace(state))
                            {
                                continue;
                            }
                        }

                        // Skip duplicate states
                        if (state == lastState)
                        {
                            _logger.LogDebug("Skipping duplicate state {State} for entity {EntityId} at {LastUpdated}.", state, stateEntry.EntityId, stateEntry.LastUpdated);
                            continue;
                        }

                        if (state is null || stateEntry.EntityId is null || stateEntry.LastChanged is null || stateEntry.LastUpdated is null)
                        {
                            _logger.LogWarning("Skipping state entry due to null values. {Json}", JsonSerializer.Serialize(stateEntry));
                            continue;
                        }

                        lastState = state;

                        var newAction = new ThermostatAction
                        {
                            EntityId = stateEntry.EntityId,
                            State = state,
                            LastChanged = stateEntry.LastChanged.Value,
                            LastUpdated = stateEntry.LastUpdated.Value
                        };

                        dbContext.ThermostatActions.Add(newAction);

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
