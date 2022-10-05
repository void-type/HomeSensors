namespace HomeSensors.Data.Repositories.Models;

public class CheckLimitResult
{
    public CheckLimitResult(TemperatureLocation location, TemperatureReading? minReading, TemperatureReading? maxReading)
    {
        Location = location;

        var success = minReading is null && maxReading is null;

        if (!success)
        {
            IsFailed = true;
        }

        MinReading = minReading;
        MaxReading = maxReading;
    }

    public TemperatureLocation Location { get; init; }
    public bool IsFailed { get; init; }
    public TemperatureReading? MinReading { get; init; }
    public TemperatureReading? MaxReading { get; init; }
}
