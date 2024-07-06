namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesResponse
{
    public TemperatureTimeSeriesResponse(TemperatureLocationResponse location, TemperatureTimeSeriesAggregate temperatureAggregate, TemperatureTimeSeriesAggregate humidityAggregate, IEnumerable<TemperatureTimeSeriesPoint> points)
    {
        Location = location;
        TemperatureAggregate = temperatureAggregate;
        HumidityAggregate = humidityAggregate;
        Points = points;
    }

    public TemperatureLocationResponse Location { get; }
    public TemperatureTimeSeriesAggregate TemperatureAggregate { get; }
    public TemperatureTimeSeriesAggregate HumidityAggregate { get; }
    public IEnumerable<TemperatureTimeSeriesPoint> Points { get; }
}
