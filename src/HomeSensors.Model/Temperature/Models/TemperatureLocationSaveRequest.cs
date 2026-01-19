namespace HomeSensors.Model.Temperature.Models;

public class TemperatureLocationSaveRequest
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public double? MinTemperatureLimitCelsius { get; init; }

    public double? MaxTemperatureLimitCelsius { get; init; }

    public bool IsHidden { get; init; }

    public long? CategoryId { get; init; }
}
