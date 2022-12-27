namespace HomeSensors.Model.Repositories.Models;

public class GraphPoint
{
    public GraphPoint(DateTimeOffset time, double? temperatureCelsius)
    {
        Time = time;
        TemperatureCelsius = temperatureCelsius;
    }

    public DateTimeOffset Time { get; }
    public double? TemperatureCelsius { get; }
}
