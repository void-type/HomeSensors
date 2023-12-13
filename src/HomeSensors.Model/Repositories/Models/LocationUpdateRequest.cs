namespace HomeSensors.Model.Repositories.Models;

public class LocationUpdateRequest
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double? MinTemperatureLimitCelsius { get; init; }
    public double? MaxTemperatureLimitCelsius { get; init; }
}
