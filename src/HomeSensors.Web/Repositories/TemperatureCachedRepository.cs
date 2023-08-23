using HomeSensors.Model.Caching;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using LazyCache;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Repositories;

/// <summary>
/// A repository that caches calls from other repositories.
/// </summary>
public class TemperatureCachedRepository : RepositoryBase
{
    private readonly TemperatureReadingRepository _readingRepository;
    private readonly IAppCache _cache;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _defaultCacheTime;
    private readonly TimeSpan _currentReadingsCacheTime;

    public TemperatureCachedRepository(TemperatureReadingRepository readingRepository, IAppCache cache,
        IDateTimeService dateTimeService, CachingSettings cachingSettings)
    {
        _readingRepository = readingRepository;
        _cache = cache;
        _dateTimeService = dateTimeService;
        _defaultCacheTime = TimeSpan.FromMinutes(cachingSettings.DefaultMinutes);
        _currentReadingsCacheTime = TimeSpan.FromSeconds(cachingSettings.CurrentReadingsSeconds);
    }

    /// <summary>
    /// If called from the timer service, then force a refresh. Otherwise all other clients will get cached data upon connection or REST query.
    /// </summary>
    /// <param name="refreshCache">When true, the cache will be refreshed.</param>
    public Task<List<Reading>> GetCurrentReadings(bool refreshCache = false)
    {
        var cacheKey = GetCaller();

        if (refreshCache)
        {
            _cache.Remove(cacheKey);
        }

        return _cache.GetOrAddAsync(cacheKey,
            _readingRepository.GetCurrent,
            _currentReadingsCacheTime);
    }

    public Task<List<GraphTimeSeries>> GetTimeSeriesReadings(GraphTimeSeriesRequest request)
    {
        // Prevent caching time spans that are incomplete
        if (request.EndTime >= _dateTimeService.MomentWithOffset)
        {
            return _readingRepository.GetTimeSeries(request);
        }

        var cacheKey = BuildCacheKey(
            GetCaller(),
            request.StartTime.ToString("o"),
            request.EndTime.ToString("o"),
            string.Join(",", request.LocationIds.OrderBy(x => x)));

        return _cache.GetOrAddAsync(cacheKey,
            async () => await _readingRepository.GetTimeSeries(request),
            // Prevent memory build up as there can be any number of keys.
            LazyCacheEntryOptions
                .WithImmediateAbsoluteExpiration(_defaultCacheTime));
    }
}
