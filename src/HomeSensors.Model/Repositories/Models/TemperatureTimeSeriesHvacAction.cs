namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesHvacAction
{
    public required string Action { get; init; }
    public required string StartTime { get; init; }
    public required string EndTime { get; init; }
}
