namespace HomeSensors.Model.TemperatureRepositories.Models;

public class GraphTimeSeries
{
    public GraphTimeSeries(string location, double? min, double? max, double? average, IEnumerable<GraphPoint> points)
    {
        Location = location;
        Min = min;
        Max = max;
        Average = average;
        Points = points;
    }

    public string Location { get; }
    public double? Min { get; }
    public double? Max { get; }
    public double? Average { get; }
    public IEnumerable<GraphPoint> Points { get; }
}
