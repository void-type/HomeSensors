using HomeSensors.Model.Cache.Configuration;
using LazyCache;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Cache;

/// <summary>
/// A helper service for working with LazyCache.
/// Implements options from configuration with default and fallback policies.
/// </summary>
public class LazyCacheOptionService
{
    /// <summary>
    /// The final fallback if custom or default options were not defined.
    /// 10 minute absolute is good for most content and taking the edge off the database.
    /// </summary>
    private static readonly CacheExpirationPolicy _fallbackPolicy = new()
    {
        Mode = CacheExpirationMode.Absolute,
        Duration = TimeSpan.FromMinutes(10)
    };

    private readonly CacheSettings _cacheSettings;

    public LazyCacheOptionService(CacheSettings cacheSettings)
    {
        _cacheSettings = cacheSettings;
    }

    /// <summary>
    /// Gets the default cache policy options from settings. If a policy or policy property is not configured, will fallback to the fallback policy properties.
    /// </summary>
    public LazyCacheEntryOptions GetOptions()
    {
        return GetPolicyFromSettings()
            .Map(GetOptions);
    }

    /// <summary>
    /// Gets a custom cache policy options from settings. If a policy or policy property is not configured, will fallback to the default then fallback policy properties.
    /// </summary>
    /// <param name="customPolicyName">The name of policy as defined in <see cref="CacheSettings"/></param>
    public LazyCacheEntryOptions GetOptions(string customPolicyName)
    {
        return GetPolicyFromSettings(customPolicyName)
            .Map(GetOptions);
    }

    /// <summary>
    /// Gets a custom cache options from the given policy.
    /// </summary>
    /// <param name="policy">A cache expiration policy</param>
    public LazyCacheEntryOptions GetOptions(CacheExpirationPolicy policy)
    {
        var mode = policy.Mode;
        var duration = policy.Duration;

        return mode switch
        {
            // Absolute expiration, lazy eviction
            CacheExpirationMode.Absolute => new LazyCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = duration
            },

            // Absolute expiration, immediate eviction
            CacheExpirationMode.ImmediateAbsolute => LazyCacheEntryOptions.WithImmediateAbsoluteExpiration(duration),

            // Sliding expiration, lazy eviction (LazyCache default)
            _ => new LazyCacheEntryOptions()
            {
                SlidingExpiration = duration
            },
        };
    }

    public CacheExpirationPolicy GetPolicyFromSettings(string? name = null)
    {
        var defaultPolicy = new CacheExpirationPolicy()
        {
            Mode = ParseMode(_cacheSettings.DefaultPolicy?.Mode) ?? _fallbackPolicy.Mode,
            Duration = ParseDurationSeconds(_cacheSettings.DefaultPolicy?.DurationSeconds) ?? _fallbackPolicy.Duration,
        };

        if (name is null)
        {
            return defaultPolicy;
        }

        var foundSettingsPolicy = _cacheSettings.CustomPolicies.TryGetValue(name, out var settingsPolicy);

        if (!foundSettingsPolicy || settingsPolicy is null)
        {
            return defaultPolicy;
        }

        return new CacheExpirationPolicy()
        {
            Mode = ParseMode(settingsPolicy.Mode) ?? defaultPolicy.Mode,
            Duration = ParseDurationSeconds(settingsPolicy.DurationSeconds) ?? defaultPolicy.Duration,
        };
    }

    private static CacheExpirationMode? ParseMode(string? modeName)
    {
        if (Enum.TryParse<CacheExpirationMode>(modeName, true, out var result))
        {
            return result;
        }

        return null;
    }

    private static TimeSpan? ParseDurationSeconds(int? durationSeconds)
    {
        if (durationSeconds is null)
        {
            return null;
        }

        return TimeSpan.FromSeconds(Convert.ToDouble(durationSeconds));
    }
}
