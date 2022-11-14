using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Web.Caching;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Temperatures;

public class CachedTemperatureRepository
{
    private readonly TemperatureReadingRepository _readingRepository;
    private readonly IAppCache _cache;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _defaultCacheTime;
    private readonly TimeSpan _currentReadingsCacheTime;

    public CachedTemperatureRepository(TemperatureReadingRepository readingRepository, IAppCache cache, IDateTimeService dateTimeService, CachingSettings cachingSettings)
    {
        _readingRepository = readingRepository;
        _cache = cache;
        _dateTimeService = dateTimeService;
        _defaultCacheTime = TimeSpan.FromMinutes(cachingSettings.DefaultMinutes);
        _currentReadingsCacheTime = TimeSpan.FromSeconds(cachingSettings.CurrentReadingsSeconds);
    }

    /// <summary>
    /// If called from the timer service, then force a refresh, otherwise all other clients will get cached data upon connection or REST query.
    /// </summary>
    /// <param name="forceRefresh">When true, the cache will be refreshed.</param>
    public async Task<List<GraphCurrentReading>> GetCurrentReadings(bool forceRefresh = false)
    {
        var cacheKey = GetCacheKeyPrefix();

        if (forceRefresh)
        {
            var item = await _readingRepository.GetCurrent();

            _cache.Add(cacheKey, item, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = _currentReadingsCacheTime,
            });

            return item;
        }

        return await _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _currentReadingsCacheTime;
                return await _readingRepository.GetCurrent();
            });
    }

    public Task<List<GraphTimeSeries>> GetTimeSeriesReadings(GraphTimeSeriesRequest request)
    {
        // Prevent caching time spans that are incomplete
        if (request.EndTime >= _dateTimeService.MomentWithOffset)
        {
            return _readingRepository.GetTimeSeries(request);
        }

        var cacheKey = $"{GetCacheKeyPrefix()}|{request.StartTime:o}|{request.EndTime:o}";

        return _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _defaultCacheTime;
                return await _readingRepository.GetTimeSeries(request);
            });
    }

    private string GetCacheKeyPrefix([CallerMemberName] string caller = "unknown")
    {
        return $"{nameof(CachedTemperatureRepository)}.{caller}";
    }
}
