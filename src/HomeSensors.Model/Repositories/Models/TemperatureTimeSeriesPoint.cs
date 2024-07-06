namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesPoint
{
    public TemperatureTimeSeriesPoint(DateTimeOffset time, double? temperatureCelsius, double? humidity)
    {
        Time = time;
        TemperatureCelsius = temperatureCelsius;
        Humidity = humidity;
    }

    public DateTimeOffset Time { get; }
    public double? TemperatureCelsius { get; }
    public double? Humidity { get; }
}
