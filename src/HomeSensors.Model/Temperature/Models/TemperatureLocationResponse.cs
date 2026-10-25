namespace HomeSensors.Model.Temperature.Models;

public class TemperatureLocationResponse(long id, string name, double? minTemperatureLimitCelsius,
    double? maxTemperatureLimitCelsius, bool isHidden, string color, long? categoryId)
{
    public long Id { get; } = id;

    public string Name { get; } = name;

    public double? MinTemperatureLimitCelsius { get; } = minTemperatureLimitCelsius;

    public double? MaxTemperatureLimitCelsius { get; } = maxTemperatureLimitCelsius;

    public bool IsHidden { get; } = isHidden;

    public string Color { get; } = color;

    public long? CategoryId { get; } = categoryId;
}
