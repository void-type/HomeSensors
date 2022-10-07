using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Temperatures;

public class CachedTemperatureRepository
{
    private readonly TemperatureReadingRepository _readingRepository;
    private readonly TemperatureDeviceRepository _deviceRepository;
    private readonly IAppCache _cache;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _defaultCacheTime = TimeSpan.FromSeconds(30);

    public CachedTemperatureRepository(TemperatureReadingRepository readingRepository, TemperatureDeviceRepository deviceRepository, IAppCache cache, IDateTimeService dateTimeService)
    {
        _readingRepository = readingRepository;
        _deviceRepository = deviceRepository;
        _cache = cache;
        _dateTimeService = dateTimeService;
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
                AbsoluteExpirationRelativeToNow = _defaultCacheTime,
            });
        }

        return await _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _defaultCacheTime;
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

    public Task<List<InactiveDevice>> GetInactiveDevices()
    {
        var cacheKey = GetCacheKeyPrefix();

        return _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _defaultCacheTime;
                return await _deviceRepository.GetInactive();
            });
    }

    public Task<List<LostDevice>> GetLostDevices()
    {
        var cacheKey = GetCacheKeyPrefix();

        return _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _defaultCacheTime;
                return await _deviceRepository.GetLost();
            });
    }

    private string GetCacheKeyPrefix([CallerMemberName] string caller = "unknown")
    {
        return $"{nameof(CachedTemperatureRepository)}.{caller}";
    }
}
