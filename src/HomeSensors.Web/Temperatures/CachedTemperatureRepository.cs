using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;
using LazyCache;

namespace HomeSensors.Web.Temperatures;

public class CachedTemperatureRepository
{
    private readonly TemperatureRepository _inner;
    private readonly IAppCache _cache;
    private readonly TimeSpan _defaultCacheTime = TimeSpan.FromSeconds(30);

    public CachedTemperatureRepository(TemperatureRepository inner, IAppCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<List<GraphTimeSeries>> GetTimeSeries(GraphTimeSeriesRequest request)
    {
        var cacheKey = $"{nameof(CachedTemperatureRepository)}.{nameof(GetTimeSeries)}|{request.StartTime:o}|{request.EndTime:o}";

        return _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _defaultCacheTime;
                return await _inner.GetTimeSeries(request);
            });
    }

    /// <summary>
    /// If called from the timer service, then force a refresh, otherwise all other clients will get cached data upon connection or REST query.
    /// </summary>
    /// <param name="forceRefresh"></param>
    public async Task<List<GraphCurrentReading>> GetCurrentReadings(bool forceRefresh = false)
    {
        var cacheKey = $"{nameof(CachedTemperatureRepository)}.{nameof(GetCurrentReadings)}";

        if (forceRefresh)
        {
            var item = await _inner.GetCurrentReadings();

            _cache.Add(cacheKey, item, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = _defaultCacheTime,
            });
        }

        return await _cache.GetOrAddAsync(cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _defaultCacheTime;
                return await _inner.GetCurrentReadings();
            });
    }
}
