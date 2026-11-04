using HomeSensors.Model.Data;
using HomeSensors.Model.Hvac.Entities;
using HomeSensors.Model.Infrastructure.HomeAssistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeSensors.Model.Hvac.Workers;

public class HvacActionsWorker : BackgroundService
{
    private readonly ILogger<HvacActionsWorker> _logger;
    private readonly HvacActionsSettings _hvacWorkerSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public HvacActionsWorker(
        ILogger<HvacActionsWorker> logger,
        HvacActionsSettings hvacWorkerSettings,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _hvacWorkerSettings = hvacWorkerSettings;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _httpClientFactory = httpClientFactory;

        logger.LogInformation("Enabling background job: {JobName} every {BetweenTicksMinutes} minutes.",
            nameof(HvacActionsWorker),
            _hvacWorkerSettings.BetweenTicksMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cursor = await LoadCursorAsync(stoppingToken);

        var betweenTicks = TimeSpan.FromMinutes(_hvacWorkerSettings.BetweenTicksMinutes);
        var timer = new PeriodicTimer(betweenTicks);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                _logger.LogInformation("{JobName} job is starting.", nameof(HvacActionsWorker));

                await SyncActionsAsync(cursor, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown in {WorkerName}.", nameof(HvacActionsWorker));
            }
            finally
            {
                _logger.LogInformation("{JobName} job is finished in {ElapsedTime}.", nameof(HvacActionsWorker), Stopwatch.GetElapsedTime(startTime));
            }
        }
    }

    private async Task<HvacCursor> LoadCursorAsync(CancellationToken stoppingToken)
    {
        // Home Assistant only goes back 10 days by default. We'll do 15 to be safe and catch anything we missed.
        var cursor = new HvacCursor
        {
            LastChanged = _timeProvider.GetUtcNow().AddDays(-15),
            LastUpdated = _timeProvider.GetUtcNow().AddDays(-15),
            LastState = "unknown"
        };

        using var scope = _scopeFactory.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        var lastEntry = await dbContext.HvacActions
            .Where(a => a.EntityId == _hvacWorkerSettings.EntityId)
            .OrderByDescending(a => a.LastChanged)
            .ThenByDescending(a => a.LastUpdated)
            .FirstOrDefaultAsync(stoppingToken);

        if (lastEntry is not null)
        {
            cursor.LastChanged = lastEntry.LastChanged;
            cursor.LastUpdated = lastEntry.LastUpdated;
            cursor.LastState = lastEntry.State;
        }

        return cursor;
    }

    private async Task SyncActionsAsync(HvacCursor cursor, CancellationToken stoppingToken)
    {
        var stateEntries = await FetchNewStateEntriesAsync(cursor, stoppingToken);

        if (stateEntries.Length == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<HomeSensorsContext>();

        foreach (var stateEntry in stateEntries)
        {
            var newAction = ToNewAction(stateEntry, cursor);

            if (newAction is null)
            {
                continue;
            }

            await dbContext.HvacActions.AddAsync(newAction, stoppingToken);

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task<EntityHistoryState[]> FetchNewStateEntriesAsync(HvacCursor cursor, CancellationToken stoppingToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("HomeAssistant");

        var startTimestamp = Uri.EscapeDataString(cursor.LastChanged.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        var endTimestamp = Uri.EscapeDataString(_timeProvider.GetUtcNow().AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ"));
        var apiUrl = $"/api/history/period/{startTimestamp}?end_time={endTimestamp}&filter_entity_id={Uri.EscapeDataString(_hvacWorkerSettings.EntityId)}";

        var response = await httpClient.GetAsync(apiUrl, stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to retrieve state history. Status: {StatusCode}, Reason: {ReasonPhrase}",
                response.StatusCode,
                response.ReasonPhrase);

            return [];
        }

        var stateHistoryList = await response.Content
            .ReadFromJsonAsync<List<List<EntityHistoryState>>>(cancellationToken: stoppingToken);

        return stateHistoryList?
            .FirstOrDefault()?
            // Get where last updated greater than our last recorded change.
            .Where(state => state.LastUpdated > cursor.LastUpdated)?
            .OrderBy(state => state.LastChanged)?
            .ThenBy(state => state.LastUpdated)?
            .ToArray()
            ?? [];
    }

    /// <summary>
    /// Advances the cursor and returns the action to store, or null if the entry should be skipped.
    /// </summary>
    private HvacAction? ToNewAction(EntityHistoryState stateEntry, HvacCursor cursor)
    {
        // Store the last changed and last updated timestamps
        if (stateEntry.LastChanged > cursor.LastChanged)
        {
            cursor.LastChanged = stateEntry.LastChanged ?? cursor.LastChanged;
        }

        if (stateEntry.LastUpdated > cursor.LastUpdated)
        {
            cursor.LastUpdated = stateEntry.LastUpdated ?? cursor.LastUpdated;
        }

        var state = stateEntry.State;

        // If it's a thermostat, we want the hvac_action attribute
        if (_hvacWorkerSettings.EntityId.StartsWith("climate.", StringComparison.Ordinal))
        {
            var hvacActionElement = stateEntry.Attributes
                .GetValueOrDefault("hvac_action", new JsonElement());

            state = hvacActionElement.ValueKind == JsonValueKind.String
                ? hvacActionElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(state))
            {
                return null;
            }
        }

        // Skip duplicate states
        if (state == cursor.LastState)
        {
            _logger.LogDebug("Skipping duplicate state {State} for entity {EntityId} at {LastUpdated}.", state, stateEntry.EntityId, stateEntry.LastUpdated);
            return null;
        }

        if (state is null || stateEntry.EntityId is null || stateEntry.LastChanged is null || stateEntry.LastUpdated is null)
        {
            _logger.LogWarning("Skipping state entry due to null values. {Json}", JsonSerializer.Serialize(stateEntry));
            return null;
        }

        cursor.LastState = state;

        return new HvacAction
        {
            EntityId = stateEntry.EntityId,
            State = state,
            LastChanged = stateEntry.LastChanged.Value,
            LastUpdated = stateEntry.LastUpdated.Value
        };
    }

    private sealed class HvacCursor
    {
        public DateTimeOffset LastChanged { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public string LastState { get; set; } = string.Empty;
    }
}
