namespace HomeSensors.Web.Health;

public class HealthChecksSettings
{
    /// <summary>
    /// Turn off the readings check when no worker records temperature readings, such as when MQTT isn't used.
    /// </summary>
    public bool IsReadingsCheckEnabled { get; init; } = true;

    public int MaxReadingAgeMinutes { get; init; } = 30;
}
