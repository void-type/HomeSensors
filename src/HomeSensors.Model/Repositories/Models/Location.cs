namespace HomeSensors.Model.Repositories.Models;

public class Location
{
    public Location(long id, string name, double? minTemperatureLimitCelsius, double? maxTemperatureLimitCelsius)
    {
        Id = id;
        Name = name;
        MinTemperatureLimitCelsius = minTemperatureLimitCelsius;
        MaxTemperatureLimitCelsius = maxTemperatureLimitCelsius;
    }

    public long Id { get; }
    public string Name { get; }
    public double? MinTemperatureLimitCelsius { get; }
    public double? MaxTemperatureLimitCelsius { get; }
}
