using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Model.Helpers;

public static class CacheHelpers
{
    public const string TemperatureDeviceAllCacheTag = "TemperatureDevice|All";
    public const string TemperatureLocationAllCacheTag = "TemperatureLocation|All";
    public const string CategoryAllCacheTag = "Category|All";

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
