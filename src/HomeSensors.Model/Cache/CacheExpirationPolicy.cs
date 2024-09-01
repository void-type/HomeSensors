namespace HomeSensors.Model.Cache;

public class CacheExpirationPolicy
{
    public CacheExpirationMode Mode { get; init; }
    public TimeSpan Duration { get; init; }
}
