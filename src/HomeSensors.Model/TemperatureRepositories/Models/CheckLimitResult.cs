namespace HomeSensors.Model.TemperatureRepositories.Models;

public class CheckLimitResult
{
    public CheckLimitResult(Location location, Reading? minReading, Reading? maxReading)
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
    public Reading? MinReading { get; }
    public Reading? MaxReading { get; }
}
