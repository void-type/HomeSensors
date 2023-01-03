using HomeSensors.Model.Data.Models;

namespace HomeSensors.Model.Repositories.Models;

public static class DataModelExtensions
{
    public static Reading ToReading(this TemperatureReading a) => new(a.Time, a.Humidity, a.TemperatureCelsius, a.TemperatureLocation?.ToLocation());

    public static Location ToLocation(this TemperatureLocation a) => new(a.Id, a.Name, a.MinTemperatureLimitCelsius, a.MaxTemperatureLimitCelsius);
}
