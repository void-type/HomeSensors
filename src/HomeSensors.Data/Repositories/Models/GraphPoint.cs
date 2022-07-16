namespace HomeSensors.Data.Repositories.Models;

public class GraphPoint
{
    public double? TemperatureCelsius { get; init; }
    public DateTimeOffset Time { get; init; }
}
