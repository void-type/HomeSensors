namespace HomeSensors.Model.TemperatureRepositories.Models;

public class GraphViewModel
{
    public IEnumerable<GraphTimeSeries> Series { get; init; } = Array.Empty<GraphTimeSeries>();
    public IEnumerable<GraphCurrentReading> Current { get; init; } = Array.Empty<GraphCurrentReading>();
}
