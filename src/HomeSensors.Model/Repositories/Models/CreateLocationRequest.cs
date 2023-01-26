namespace HomeSensors.Model.Repositories.Models;

public class CreateLocationRequest
{
    public string Name { get; init; } = string.Empty;
    public double? MinTemperatureLimitCelsius { get; init; }
    public double? MaxTemperatureLimitCelsius { get; init; }
}
