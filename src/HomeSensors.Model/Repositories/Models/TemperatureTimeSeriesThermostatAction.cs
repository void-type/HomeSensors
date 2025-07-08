namespace HomeSensors.Model.Repositories.Models;

public class TemperatureTimeSeriesThermostatAction
{
    public required string Action { get; init; }
    public required string StartTime { get; init; }
    public required string EndTime { get; init; }
}
