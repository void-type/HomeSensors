namespace HomeSensors.Model.Repositories.Models;

public class TimeSeries
{
    public TimeSeries(Location location, TimeSeriesAggregate temperatureAggregate, TimeSeriesAggregate humidityAggregate, IEnumerable<TimeSeriesPoint> points)
    {
        Location = location;
        TemperatureAggregate = temperatureAggregate;
        HumidityAggregate = humidityAggregate;
        Points = points;
    }

    public Location Location { get; }
    public TimeSeriesAggregate TemperatureAggregate { get; }
    public TimeSeriesAggregate HumidityAggregate { get; }
    public IEnumerable<TimeSeriesPoint> Points { get; }
}
