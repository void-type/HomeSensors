using HomeSensors.Model.TemperatureRepositories;
using HomeSensors.Model.TemperatureRepositories.Models;
using HomeSensors.Web.Caching;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Temperatures;

public class CachedTemperatureRepository
{
    private readonly TemperatureReadingRepository _readingRepository;
    private readonly TemperatureLocationRepository _locationRepository;
    private readonly IAppCache _cache;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _currentReadingsCacheTime;

    public CachedTemperatureRepository(TemperatureReadingRepository readingRepository, IAppCache cache,
        IDateTimeService dateTimeService, CachingSettings cachingSettings, TemperatureLocationRepository locationRepository)
    {
        _readingRepository = readingRepository;
        _cache = cache;
        _dateTimeService = dateTimeService;
        _locationRepository = locationRepository;
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

        var cacheKey = BuildCacheKey(
            GetCacheKeyPrefix(),
            request.StartTime.ToString("o"),
            request.EndTime.ToString("o"),
            string.Join(",", request.LocationIds.OrderBy(x => x)),
            GetCacheKey(request.PaginationOptions));

        return _cache.GetOrAddAsync(cacheKey,
            async _ => await _readingRepository.GetTimeSeries(request));
    }

    public Task<List<Location>> GetAllLocations()
    {
        var cacheKey = GetCacheKeyPrefix();

        return _cache.GetOrAddAsync(cacheKey,
            async _ => await _locationRepository.GetAll(PaginationOptions.None));
    }

    private string GetCacheKeyPrefix([CallerMemberName] string caller = "unknown")
    {
        return $"{nameof(CachedTemperatureRepository)}.{caller}";
    }

    private static string BuildCacheKey(params string[] cacheKeyParts)
    {
        return string.Join("|", cacheKeyParts);
    }

    private static string GetCacheKey(PaginationOptions paginationOptions)
    {
        return paginationOptions.IsPagingEnabled
            ? $"{paginationOptions.Page},{paginationOptions.Take}"
            : "None";
    }
}
