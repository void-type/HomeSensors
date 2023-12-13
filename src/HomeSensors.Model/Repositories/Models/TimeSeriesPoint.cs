namespace HomeSensors.Model.Repositories.Models;

public class TimeSeriesPoint
{
    public TimeSeriesPoint(DateTimeOffset time, double? temperatureCelsius)
    {
        Time = time;
        TemperatureCelsius = temperatureCelsius;
    }

    public DateTimeOffset Time { get; }
    public double? TemperatureCelsius { get; }
}
