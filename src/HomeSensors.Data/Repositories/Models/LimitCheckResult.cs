namespace HomeSensors.Data.Repositories.Models;

public class LimitCheckResult
{
    public LimitCheckResult(TemperatureLocation location, TemperatureReading? minReading, TemperatureReading? maxReading)
    {
        Location = location;
        MinReading = minReading;
        MaxReading = maxReading;
    }

    public TemperatureLocation Location { get; init; }
    public TemperatureReading? MinReading { get; init; }
    public TemperatureReading? MaxReading { get; init; }
}
