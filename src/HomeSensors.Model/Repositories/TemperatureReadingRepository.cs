﻿using HomeSensors.Model.Data;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureReadingRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;
    private readonly IDateTimeService _dateTimeService;
    private readonly HybridCache _cache;

    public TemperatureReadingRepository(HomeSensorsContext data, IDateTimeService dateTimeService, HybridCache cache)
    {
        _data = data;
        _dateTimeService = dateTimeService;
        _cache = cache;
    }

    /// <summary>
    /// Gets the latest reading from each location. Limited to locations that have readings within the last 24 hours.
    /// </summary>
    public async Task<List<TemperatureReadingResponse>> GetCurrent(CancellationToken cancellationToken = default)
    {
        var data = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.Time >= _dateTimeService.MomentWithOffset.AddDays(-1))
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(g => g.Key != "Outside")
            .ThenBy(x => x.Key)
            .Select(g => g.OrderByDescending(x => x.Time).First())
            .ToListAsync(cancellationToken);

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
    public async Task<List<TemperatureReadingResponse>> GetCurrentCached(bool refreshCache = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCaller();

        if (refreshCache)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await GetCurrent(cancel),
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
    public Task<Maybe<TemperatureReadingResponse>> GetCurrentForLocation(long locationId, CancellationToken cancellationToken = default)
    {
        return _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId == locationId)
            .OrderByDescending(x => x.Time)
            .FirstOrDefaultAsync(cancellationToken)
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
    public async Task<Maybe<TemperatureReadingResponse>> GetCurrentForLocationCached(long locationId, CancellationToken cancellationToken = default)
    {
        var caller = GetCaller();
        var cacheKey = $"{caller}|{locationId}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await GetCurrentForLocation(locationId, cancel),
            options: new()
            {
                Expiration = TimeSpan.FromSeconds(60),
                LocalCacheExpiration = TimeSpan.FromSeconds(60),
            },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">TemperatureTimeSeriesRequest</param>
    public async Task<List<TemperatureTimeSeriesResponse>> GetTimeSeries(TemperatureTimeSeriesRequest request, CancellationToken cancellationToken = default)
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
    public async Task<List<TemperatureTimeSeriesResponse>> GetTimeSeriesCached(TemperatureTimeSeriesRequest request, CancellationToken cancellationToken = default)
    {
        // Prevent caching incomplete series to stay current.
        if (request.EndTime >= _dateTimeService.MomentWithOffset)
        {
            return await GetTimeSeries(request, cancellationToken);
        }

        var caller = GetCaller();
        var cacheKey = $"{caller}|{request.StartTime:o}|{request.EndTime:o}|{string.Join(",", request.LocationIds.OrderBy(x => x))}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async (cancel) => await GetTimeSeries(request, cancel),
            cancellationToken: cancellationToken);
    }
}
