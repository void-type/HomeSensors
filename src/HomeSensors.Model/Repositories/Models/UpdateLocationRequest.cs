namespace HomeSensors.Model.Repositories.Models;

public class UpdateLocationRequest
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double? MinLimitTemperatureCelsius { get; init; }
    public double? MaxLimitTemperatureCelsius { get; init; }
}
