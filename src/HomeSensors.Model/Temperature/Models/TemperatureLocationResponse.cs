namespace HomeSensors.Model.Temperature.Models;

public class TemperatureLocationResponse
{
    public TemperatureLocationResponse(long id, string name, double? minTemperatureLimitCelsius,
        double? maxTemperatureLimitCelsius, bool isHidden, string color, long? categoryId)
    {
        Id = id;
        Name = name;
        MinTemperatureLimitCelsius = minTemperatureLimitCelsius;
        MaxTemperatureLimitCelsius = maxTemperatureLimitCelsius;
        IsHidden = isHidden;
        Color = color;
        CategoryId = categoryId;
    }

    public long Id { get; }

    public string Name { get; }

    public double? MinTemperatureLimitCelsius { get; }

    public double? MaxTemperatureLimitCelsius { get; }

    public bool IsHidden { get; }

    public string Color { get; }

    public long? CategoryId { get; }
}
