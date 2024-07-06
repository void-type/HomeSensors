namespace HomeSensors.Model.Repositories.Models;

public class TemperatureCheckLimitResponse
{
    public TemperatureCheckLimitResponse(TemperatureLocationResponse location, TemperatureReadingResponse? minReading, TemperatureReadingResponse? maxReading)
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

    public TemperatureLocationResponse Location { get; }
    public bool IsFailed { get; }
    public TemperatureReadingResponse? MinReading { get; }
    public TemperatureReadingResponse? MaxReading { get; }
}
