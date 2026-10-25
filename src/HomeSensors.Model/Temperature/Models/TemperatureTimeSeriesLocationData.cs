namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesLocationData(TemperatureLocationResponse location, TemperatureTimeSeriesAggregate temperatureAggregate, TemperatureTimeSeriesAggregate humidityAggregate, List<TemperatureTimeSeriesPoint> points)
{
    public TemperatureLocationResponse Location { get; } = location;

    public TemperatureTimeSeriesAggregate TemperatureAggregate { get; } = temperatureAggregate;

    public TemperatureTimeSeriesAggregate HumidityAggregate { get; } = humidityAggregate;

    public List<TemperatureTimeSeriesPoint> Points { get; } = points;
}
