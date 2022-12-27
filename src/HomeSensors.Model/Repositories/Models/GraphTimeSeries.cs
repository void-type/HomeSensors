namespace HomeSensors.Model.Repositories.Models;

public class GraphTimeSeries
{
    public GraphTimeSeries(Location location, double? min, double? max, double? average, IEnumerable<GraphPoint> points)
    {
        Location = location;
        MinTemperatureCelsius = min;
        MaxTemperatureCelsius = max;
        AverageTemperatureCelsius = average;
        Points = points;
    }

    public Location Location { get; }
    public double? MinTemperatureCelsius { get; }
    public double? MaxTemperatureCelsius { get; }
    public double? AverageTemperatureCelsius { get; }
    public IEnumerable<GraphPoint> Points { get; }
}
