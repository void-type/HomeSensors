namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesPoint(DateTimeOffset time, double? temperatureCelsius, double? humidity)
{
    public DateTimeOffset Time { get; } = time;

    public double? TemperatureCelsius { get; } = temperatureCelsius;

    public double? Humidity { get; } = humidity;
}
