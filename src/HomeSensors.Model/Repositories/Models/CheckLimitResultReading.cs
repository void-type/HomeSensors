namespace HomeSensors.Model.Repositories.Models;

public class CheckLimitResultReading
{
    public CheckLimitResultReading(long id, DateTimeOffset time, double? temperatureCelsius)
    {
        Id = id;
        Time = time;
        TemperatureCelsius = temperatureCelsius;
    }

    public long Id { get; }
    public DateTimeOffset Time { get; }
    public double? TemperatureCelsius { get; }
}
