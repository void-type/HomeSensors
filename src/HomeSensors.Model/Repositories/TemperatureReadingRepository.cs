using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureReadingRepository : RepositoryBase
{
    private readonly IDbContextFactory<HomeSensorsContext> _contextFactory;
    private readonly IDateTimeService _dateTimeService;
    private readonly HybridCache _cache;

    public TemperatureReadingRepository(IDbContextFactory<HomeSensorsContext> contextFactory, IDateTimeService dateTimeService, HybridCache cache)
    {
        _contextFactory = contextFactory;
        _dateTimeService = dateTimeService;
        _cache = cache;
    }

    /// <summary>
    /// Gets the latest reading from each location. Limited to locations that have readings within the last 24 hours.
    /// </summary>
    /// <param name="refreshCache">Pass true to force refresh of the cache. Use when scheduled and all other clients can use same cached interval.</param>
    public async Task<List<TemperatureReadingResponse>> GetCurrentCachedAsync(bool refreshCache = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCaller();

        if (refreshCache)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await GetCurrentAsync(cancel),
            options: new()
            {
                Expiration = TimeSpan.FromSeconds(60),
                LocalCacheExpiration = TimeSpan.FromSeconds(60),
            },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the latest reading for the location.
    /// </summary>
    public async Task<Maybe<TemperatureReadingResponse>> GetCurrentForLocationCachedAsync(long locationId, CancellationToken cancellationToken = default)
    {
        return (await GetCurrentCachedAsync(cancellationToken: cancellationToken))
            .FirstOrDefault(x => x.Location?.Id == locationId);
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">TemperatureTimeSeriesRequest</param>
    public async Task<List<TemperatureTimeSeriesLocationData>> GetTimeSeriesCachedAsync(TemperatureTimeSeriesRequest request, CancellationToken cancellationToken = default)
    {
        // Prevent caching incomplete series to stay current.
        if (request.EndTime >= _dateTimeService.MomentWithOffset)
        {
            return await GetTimeSeriesAsync(request, cancellationToken);
        }

        var caller = GetCaller();
        var cacheKey = $"{caller}|{request.StartTime:o}|{request.EndTime:o}|{string.Join(",", request.LocationIds.Order())}|{request.IncludeHvacActions}|{request.TrimHvacActionsToRequestedTimeRange}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async (cancel) => await GetTimeSeriesAsync(request, cancel),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Pull a time series of HVAC actions.
    /// </summary>
    /// <param name="request">TemperatureTimeSeriesRequest</param>
    public async Task<List<TemperatureTimeSeriesHvacAction>> GetHvacActionsCachedAsync(TemperatureTimeSeriesRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.IncludeHvacActions)
        {
            return [];
        }

        // Prevent caching incomplete series to stay current.
        if (request.EndTime >= _dateTimeService.MomentWithOffset)
        {
            return await GetHvacActionsAsync(request, cancellationToken);
        }

        var caller = GetCaller();
        var cacheKey = $"{caller}|{request.StartTime:o}|{request.EndTime:o}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async (cancel) => await GetHvacActionsAsync(request, cancel),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the latest reading from each location. Limited to locations that have readings within the last 24 hours.
    /// </summary>
    private async Task<List<TemperatureReadingResponse>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var data = await context.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .ThenInclude(x => x!.Category)
            .Where(x => !x.IsSummary && x.Time >= _dateTimeService.MomentWithOffset.AddDays(-1))
            .GroupBy(x => x.TemperatureLocationId)
            .Select(g => g.OrderByDescending(x => x.Time).First())
            .ToListAsync(cancellationToken);

        var orderedData = data
            .OrderBy(x => x.TemperatureLocation!.Category!.Order)
            .ThenBy(x => x.TemperatureLocation!.Name)
            .ToList();

        return orderedData.ConvertAll(x => new TemperatureReadingResponse(
            time: x.Time,
            humidity: x.Humidity,
            temperatureCelsius: x.TemperatureCelsius,
            location: x.TemperatureLocation!.ToApiResponse()
        ));
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">TemperatureTimeSeriesRequest</param>
    private async Task<List<TemperatureTimeSeriesLocationData>> GetTimeSeriesAsync(TemperatureTimeSeriesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LocationIds.Count == 0)
        {
            return [];
        }

        await using var context = _contextFactory.CreateDbContext();

        var dbReadings = await context.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .ThenInclude(x => x!.Category)
            .Where(x => request.LocationIds.Contains(x.TemperatureLocationId))
            .Where(x => x.Time >= request.StartTime && x.Time <= request.EndTime)
            .OrderByDescending(x => x.Time)
            .ToListAsync(cancellationToken);

        if (dbReadings.Count == 0)
        {
            return [];
        }

        // Depending on the total span of data returned, we will create averages over intervals to prevent overloading the client with too many data points.
        var dbSpanHours = (dbReadings.Max(x => x.Time) - dbReadings.Min(x => x.Time)).TotalHours;

        var intervalMinutes = dbSpanHours switch
        {
            > 3 * 24 => 60,
            > 2 * 24 => 30,
            > 1 * 24 => 15,
            > 12 => 10,
            > 6 => 5,
            > 3 => 1,
            _ => 0,
        };

        return dbReadings
            .OrderBy(x => x.TemperatureLocation!.Category!.Order)
            .ThenBy(x => x.TemperatureLocation!.Name)
            .GroupBy(x => x.TemperatureLocation!.Name)
            .ToList()
            .ConvertAll(readingsForLocation =>
            {
                var avgGraphPoints = readingsForLocation.GetIntervalAverages(intervalMinutes);

                var allTemps = readingsForLocation.Select(x => x.TemperatureCelsius);

                var tempAgg = new TemperatureTimeSeriesAggregate(
                    minimum: allTemps.Min(),
                    maximum: allTemps.Max(),
                    average: allTemps.Average());

                var allHumidity = readingsForLocation.Select(x => x.Humidity);

                var humidityAgg = new TemperatureTimeSeriesAggregate(
                    minimum: allHumidity.Min(),
                    maximum: allHumidity.Max(),
                    average: allHumidity.Average());

                return new TemperatureTimeSeriesLocationData(
                    location: readingsForLocation.First().TemperatureLocation!.ToApiResponse(),
                    tempAgg,
                    humidityAgg,
                    points: avgGraphPoints
                );
            });
    }

    private async Task<List<TemperatureTimeSeriesHvacAction>> GetHvacActionsAsync(TemperatureTimeSeriesRequest request, CancellationToken cancellationToken)
    {
        await using var context = _contextFactory.CreateDbContext();

        var dbReadings = await context.HvacActions
           .TagWith(GetTag())
           .AsNoTracking()
           .Where(x => x.LastUpdated >= request.StartTime && x.LastUpdated <= request.EndTime)
           .OrderBy(x => x.LastUpdated)
           .ToListAsync(cancellationToken);

        // Get the most recent action before the start time, so the chart can show this period ending.
        var firstExtraAction = await context.HvacActions
           .TagWith(GetTag())
           .AsNoTracking()
           .Where(x => x.LastUpdated < request.StartTime)
           .OrderByDescending(x => x.LastUpdated)
           .FirstOrDefaultAsync(cancellationToken);

        // Get the earliest action after the end time, so the chart can show this period starting.
        var lastExtraAction = await context.HvacActions
           .TagWith(GetTag())
           .AsNoTracking()
           .Where(x => x.LastUpdated > request.EndTime)
           .OrderBy(x => x.LastUpdated)
           .FirstOrDefaultAsync(cancellationToken);

        dbReadings = firstExtraAction != null
            ? [firstExtraAction, .. dbReadings]
            : dbReadings;

        dbReadings = lastExtraAction != null
            ? [.. dbReadings, lastExtraAction]
            : dbReadings;

        dbReadings = [.. dbReadings.OrderBy(x => x.LastUpdated)];

        if (dbReadings.Count == 0)
        {
            return [];
        }

        var result = new List<TemperatureTimeSeriesHvacAction>();

        // If last extra action exists, then we are likely in the past, so we'll treat it as a possible end to our last action.
        // Else we might be at the end of our data, so we'll stop a last action at the end of of the queried period.
        var hasLastExtraAction = lastExtraAction is not null;
        var count = hasLastExtraAction ? dbReadings.Count - 1 : dbReadings.Count;

        // Iterate through the actions and build the time series.
        // Thermostat actions have a time stamp that denotes the action period start. We need to get the end timestamp from the next item.
        // We are only interested in returning heating and cooling periods.
        for (var i = 0; i < count; i++)
        {
            var action = dbReadings[i];
            var nextAction = dbReadings.Count > i + 1 ? dbReadings[i + 1] : null;

            if (action.State == HvacAction.Heating || action.State == HvacAction.Cooling)
            {
                var startTime = action.LastUpdated;

                var endTime = nextAction is not null
                    ? nextAction.LastUpdated
                    : _dateTimeService.MomentWithOffset;

                if (endTime <= startTime)
                {
                    // If the end time is before the start time (action is in the future and hasn't ended), skip this action because we don't want to make up data.
                    continue;
                }

                // Respect the requested time range.
                if (request.TrimHvacActionsToRequestedTimeRange)
                {
                    if (startTime < request.StartTime)
                    {
                        startTime = request.StartTime;
                    }

                    if (endTime > request.EndTime)
                    {
                        endTime = request.EndTime;
                    }

                    if (endTime <= startTime)
                    {
                        continue;
                    }
                }

                result.Add(new TemperatureTimeSeriesHvacAction
                {
                    Action = action.State,
                    StartTime = startTime,
                    EndTime = endTime,
                    DurationMinutes = (int)(endTime - startTime).TotalMinutes,
                });
            }
        }

        return result;
    }
}
