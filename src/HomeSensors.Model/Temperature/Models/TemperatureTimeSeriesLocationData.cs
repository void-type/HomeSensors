namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesLocationData
{
    public TemperatureTimeSeriesLocationData(TemperatureLocationResponse location, TemperatureTimeSeriesAggregate temperatureAggregate, TemperatureTimeSeriesAggregate humidityAggregate, List<TemperatureTimeSeriesPoint> points)
    {
        Location = location;
        TemperatureAggregate = temperatureAggregate;
        HumidityAggregate = humidityAggregate;
        Points = points;
    }

    public TemperatureLocationResponse Location { get; }

    public TemperatureTimeSeriesAggregate TemperatureAggregate { get; }

    public TemperatureTimeSeriesAggregate HumidityAggregate { get; }

    public List<TemperatureTimeSeriesPoint> Points { get; }
}
