namespace HomeSensors.Model.Cache.Configuration;

public class CacheSettingsExpirationPolicy
{
    /// <summary>
    /// See <see cref="CacheExpirationMode"/> for available options. If null, will fallback to Default then hardcoded fallback.
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>
    /// The cache duration of the entry. If null, will fallback to Default then hardcoded fallback.
    /// </summary>
    public int? DurationSeconds { get; init; }
}
