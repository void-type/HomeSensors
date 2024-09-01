namespace HomeSensors.Model.Cache.Configuration;

public class CacheSettings
{
    public CacheSettingsExpirationPolicy? DefaultPolicy { get; init; }

    public Dictionary<string, CacheSettingsExpirationPolicy?> CustomPolicies { get; init; } = [];
}
