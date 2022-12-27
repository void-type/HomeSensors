namespace HomeSensors.Model.Repositories.Models;

public class Location
{
    public Location(long id, string name, double? minLimitTemperatureCelsius, double? maxLimitTemperatureCelsius)
    {
        Id = id;
        Name = name;
        MinLimitTemperatureCelsius = minLimitTemperatureCelsius;
        MaxLimitTemperatureCelsius = maxLimitTemperatureCelsius;
    }

    public long Id { get; }
    public string Name { get; }
    public double? MinLimitTemperatureCelsius { get; }
    public double? MaxLimitTemperatureCelsius { get; }
}
