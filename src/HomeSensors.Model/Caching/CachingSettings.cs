namespace HomeSensors.Model.Caching;

public class CachingSettings
{
    public int DefaultMinutes { get; init; } = 5;
    public int CurrentReadingsSeconds { get; init; } = 60;
}
