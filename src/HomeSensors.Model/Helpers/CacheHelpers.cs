using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Model.Helpers;

public static class CacheHelpers
{
    public static string BuildCacheKey(params object[] cacheKeyParts)
    {
        return string.Join("|", cacheKeyParts.ToString());
    }

    public static string GetCacheKey(this PaginationOptions paginationOptions)
    {
        return paginationOptions.IsPagingEnabled
            ? $"{paginationOptions.Page},{paginationOptions.Take}"
            : "None";
    }
}
