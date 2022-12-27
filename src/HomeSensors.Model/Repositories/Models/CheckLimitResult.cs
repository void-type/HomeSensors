namespace HomeSensors.Model.Repositories.Models;

public class CheckLimitResult
{
    public CheckLimitResult(Location location, CheckLimitResultReading? minReading, CheckLimitResultReading? maxReading)
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

    public Location Location { get; }
    public bool IsFailed { get; }
    public CheckLimitResultReading? MinReading { get; }
    public CheckLimitResultReading? MaxReading { get; }
}
