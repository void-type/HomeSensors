namespace HomeSensors.Model.Repositories.Models;

public class TemperatureLocationResponse
{
    public TemperatureLocationResponse(long id, string name, double? minTemperatureLimitCelsius,
        double? maxTemperatureLimitCelsius, bool isHidden, long? categoryId)
    {
        Id = id;
        Name = name;
        MinTemperatureLimitCelsius = minTemperatureLimitCelsius;
        MaxTemperatureLimitCelsius = maxTemperatureLimitCelsius;
        IsHidden = isHidden;
        CategoryId = categoryId;
    }

    public long Id { get; }

    public string Name { get; }

    public double? MinTemperatureLimitCelsius { get; }

    public double? MaxTemperatureLimitCelsius { get; }

    public bool IsHidden { get; }

    public long? CategoryId { get; }
}
