using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Model.Caching;

public static class CachingExtensions
{
    public static string GetCacheKey(this PaginationOptions paginationOptions)
    {
        return paginationOptions.IsPagingEnabled
            ? $"{paginationOptions.Page},{paginationOptions.Take}"
            : "None";
    }
}
