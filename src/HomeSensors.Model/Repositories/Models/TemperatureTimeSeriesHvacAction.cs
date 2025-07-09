namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesHvacAction
{
    public required string Action { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
}
