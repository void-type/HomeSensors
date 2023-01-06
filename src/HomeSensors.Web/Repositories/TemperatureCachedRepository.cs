using HomeSensors.Model.Caching;
using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Time;

namespace HomeSensors.Web.Repositories;

/// <summary>
/// A repository that caches calls from other repositories.
/// </summary>
public class TemperatureCachedRepository : CachedRepositoryBase
{
    private readonly TemperatureReadingRepository _readingRepository;
    private readonly TemperatureLocationRepository _locationRepository;
    private readonly TemperatureDeviceRepository _deviceRepository;
    private readonly IAppCache _cache;
    private readonly IDateTimeService _dateTimeService;
    private readonly TimeSpan _currentReadingsCacheTime;

    public TemperatureCachedRepository(TemperatureReadingRepository readingRepository, IAppCache cache, IDateTimeService dateTimeService,
        CachingSettings cachingSettings, TemperatureLocationRepository locationRepository, TemperatureDeviceRepository deviceRepository)
    {
        _readingRepository = readingRepository;
        _cache = cache;
        _dateTimeService = dateTimeService;
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
        _currentReadingsCacheTime = TimeSpan.FromSeconds(cachingSettings.CurrentReadingsSeconds);
    }

    /// <summary>
    /// If called from the timer service, then force a refresh. Otherwise all other clients will get cached data upon connection or REST query.
    /// </summary>
    /// <param name="forceRefresh">When true, the cache will be refreshed.</param>
    public async Task<List<Reading>> GetCurrentReadings(bool forceRefresh = false)
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
            string.Join(",", request.LocationIds.OrderBy(x => x)));

        return _cache.GetOrAddAsync(cacheKey,
            async _ => await _readingRepository.GetTimeSeries(request));
    }

    public Task<List<Location>> GetAllLocations()
    {
        var cacheKey = GetCacheKeyPrefix();

        return _cache.GetOrAddAsync(cacheKey,
            async _ => await _locationRepository.GetAll());
    }

    public Task<List<Device>> GetAllDevices()
    {
        var cacheKey = GetCacheKeyPrefix();

        return _cache.GetOrAddAsync(cacheKey,
            async _ => await _deviceRepository.GetAll());
    }

    public Task<IResult<EntityMessage<long>>> UpdateDevice(UpdateDeviceRequest request)
    {
        _cache.Remove(GetCacheKeyPrefix(nameof(GetAllDevices)));

        return _deviceRepository.Update(request);
    }

    public Task<IResult<EntityMessage<long>>> CreateLocation(CreateLocationRequest request)
    {
        _cache.Remove(GetCacheKeyPrefix(nameof(GetAllLocations)));

        return _locationRepository.Create(request);
    }

    public Task<IResult<EntityMessage<long>>> UpdateLocation(UpdateLocationRequest request)
    {
        _cache.Remove(GetCacheKeyPrefix(nameof(GetAllLocations)));

        return _locationRepository.Update(request);
    }
}
