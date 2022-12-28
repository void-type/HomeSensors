using HomeSensors.Model.Repositories;
using System.Runtime.CompilerServices;

namespace HomeSensors.Model.Caching;

public class CachedRepositoryBase : RepositoryBase
{
    protected static string BuildCacheKey(params string[] cacheKeyParts)
    {
        return string.Join("|", cacheKeyParts);
    }

    protected string GetCacheKeyPrefix([CallerMemberName] string caller = "unknown")
    {
        return $"{ThisClassName}.{caller}";
    }
}
