namespace HomeSensors.Model.Repositories.Models;

public class TemperatureLocationCreateRequest
{
    public string Name { get; init; } = string.Empty;
    public double? MinTemperatureLimitCelsius { get; init; }
    public double? MaxTemperatureLimitCelsius { get; init; }
}
