using HomeSensors.Model.Caching;
using LazyCache;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

// TODO: make this a services to inject. Wrap caching and have dependency keys.
public class CachedRepositoryBase : RepositoryBase
{
    public CachedRepositoryBase(CachingSettings cacheSettings, IAppCache cache, IDateTimeService dateTimeService)
    {
        CacheSettings = cacheSettings;
        Cache = cache;
        DateTimeService = dateTimeService;
    }

    protected CachingSettings CacheSettings { get; }
    protected IAppCache Cache { get; }
    protected IDateTimeService DateTimeService { get; }

    protected static string BuildCacheKey(params string[] cacheKeyParts)
    {
        return string.Join("|", cacheKeyParts);
    }

    protected DateTimeOffset GetAbsoluteCacheExpiration(TimeSpan relativeExpiration)
    {
        return DateTimeService.MomentWithOffset.Add(relativeExpiration);
    }
}
