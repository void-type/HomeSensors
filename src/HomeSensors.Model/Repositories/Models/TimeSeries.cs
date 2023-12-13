namespace HomeSensors.Model.Repositories.Models;

public class TimeSeries
{
    public TimeSeries(Location location, double? min, double? max, double? average, IEnumerable<TimeSeriesPoint> points)
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
    public IEnumerable<TimeSeriesPoint> Points { get; }
}
