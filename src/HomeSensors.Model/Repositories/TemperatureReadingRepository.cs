using HomeSensors.Model.Caching;
using HomeSensors.Model.Data;
using HomeSensors.Model.Repositories.Models;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureReadingRepository : CachedRepositoryBase
{
    private readonly HomeSensorsContext _data;

    public TemperatureReadingRepository(CachingSettings cachingSettings, HomeSensorsContext data,
        IDateTimeService dateTimeService, IAppCache cache)
        : base(cachingSettings, cache, dateTimeService)
    {
        _data = data;
    }

    /// <summary>
    /// Gets the latest reading from each location. Limited to locations that have readings within the last 24 hours.
    /// </summary>
    public async Task<List<TemperatureReadingResponse>> GetCurrent()
    {
        var data = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.Time >= DateTimeService.MomentWithOffset.AddDays(-1))
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(g => g.Key != "Outside")
            .ThenBy(x => x.Key)
            .Select(g => g.OrderByDescending(x => x.Time).First())
            .ToListAsync();

        return data.ConvertAll(x => new TemperatureReadingResponse(
            time: x.Time,
            humidity: x.Humidity,
            temperatureCelsius: x.TemperatureCelsius,
            location: x.TemperatureLocation!.ToApiResponse()
        ));
    }

    /// <summary>
    /// Gets the latest reading from each location. Limited to locations that have readings within the last 24 hours.
    /// </summary>
    /// <param name="refreshCache">Pass true to force refresh of the cache. Use when scheduled and all other clients can use same cached interval.</param>
    public Task<List<TemperatureReadingResponse>> GetCurrentCached(bool refreshCache = false)
    {
        var cacheKey = GetCaller();

        if (refreshCache)
        {
            Cache.Remove(cacheKey);
        }

        return Cache.GetOrAddAsync(
            cacheKey,
            GetCurrent,
            GetAbsoluteCacheExpiration(CacheSettings.CurrentReadingsCacheTime));
    }

    /// <summary>
    /// Gets the latest reading for the location.
    /// </summary>
    public Task<Maybe<TemperatureReadingResponse>> GetCurrentForLocation(long locationId)
    {
        return _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId == locationId)
            .OrderByDescending(x => x.Time)
            .FirstOrDefaultAsync()
            .MapAsync(Maybe.From)
            .SelectAsync(x => new TemperatureReadingResponse(
                time: x.Time,
                humidity: x.Humidity,
                temperatureCelsius: x.TemperatureCelsius,
                location: x.TemperatureLocation!.ToApiResponse()));
    }

    /// <summary>
    /// Gets the latest reading for the location.
    /// </summary>
    public Task<Maybe<TemperatureReadingResponse>> GetCurrentForLocationCached(long locationId)
    {
        var cacheKey = BuildCacheKey(
            GetCaller(),
            locationId.ToString());

        return Cache.GetOrAddAsync(
            cacheKey,
            async () => await GetCurrentForLocation(locationId),
            GetAbsoluteCacheExpiration(CacheSettings.CurrentReadingsCacheTime));
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">TemperatureTimeSeriesRequest</param>
    public async Task<List<TemperatureTimeSeriesResponse>> GetTimeSeries(TemperatureTimeSeriesRequest request)
    {
        if (request.LocationIds.Count == 0)
        {
            return [];
        }

        var dbReadings = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => request.LocationIds.Contains(x.TemperatureLocationId))
            .Where(x => x.Time >= request.StartTime && x.Time <= request.EndTime)
            .OrderByDescending(x => x.Time)
            .ToListAsync();

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
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(x => x.Key != "Outside")
            .ThenBy(x => x.Key)
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

                return new TemperatureTimeSeriesResponse(
                    location: readingsForLocation.First().TemperatureLocation!.ToApiResponse(),
                    tempAgg,
                    humidityAgg,
                    points: avgGraphPoints
                );
            });
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">TemperatureTimeSeriesRequest</param>
    public Task<List<TemperatureTimeSeriesResponse>> GetTimeSeriesCached(TemperatureTimeSeriesRequest request)
    {
        // Prevent caching time spans that are incomplete
        if (request.EndTime >= DateTimeService.MomentWithOffset)
        {
            return GetTimeSeries(request);
        }

        var cacheKey = BuildCacheKey(
            GetCaller(),
            request.StartTime.ToString("o"),
            request.EndTime.ToString("o"),
            string.Join(",", request.LocationIds.OrderBy(x => x)));

        return Cache.GetOrAddAsync(
            cacheKey,
            async () => await GetTimeSeries(request),
            // Prevent memory build up as there can be any number of keys.
            LazyCacheEntryOptions.WithImmediateAbsoluteExpiration(CacheSettings.DefaultCacheTime));
    }
}
