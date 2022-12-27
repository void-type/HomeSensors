using System.Runtime.CompilerServices;

namespace HomeSensors.Model.Caching;

public class CachedRepositoryBase
{
    private readonly string _thisClassName;

    public CachedRepositoryBase()
    {
        _thisClassName = GetType().Name;
    }

    protected static string BuildCacheKey(params string[] cacheKeyParts)
    {
        return string.Join("|", cacheKeyParts);
    }

    protected string GetCacheKeyPrefix([CallerMemberName] string caller = "unknown")
    {
        return $"{_thisClassName}.{caller}";
    }
}
