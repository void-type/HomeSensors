namespace HomeSensors.Model.Caching;

public class CachingSettings
{
    public int DefaultMinutes { get; init; } = 5;

    public TimeSpan DefaultCacheTime => TimeSpan.FromMinutes(DefaultMinutes);

    public int CurrentReadingsSeconds { get; init; } = 60;

    public TimeSpan CurrentReadingsCacheTime => TimeSpan.FromMinutes(CurrentReadingsSeconds);
}
