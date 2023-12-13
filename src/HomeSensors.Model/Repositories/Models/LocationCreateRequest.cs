namespace HomeSensors.Model.Repositories.Models;

public class LocationCreateRequest
{
    public string Name { get; init; } = string.Empty;
    public double? MinTemperatureLimitCelsius { get; init; }
    public double? MaxTemperatureLimitCelsius { get; init; }
}
