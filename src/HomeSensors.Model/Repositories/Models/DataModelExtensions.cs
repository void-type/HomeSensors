using HomeSensors.Model.Data.Models;

namespace HomeSensors.Model.Repositories.Models;

public static class DataModelExtensions
{
    public static CheckLimitResultReading ToCheckLimitResultReading(this TemperatureReading a) => new(a.Id, a.Time, a.TemperatureCelsius);

    public static Location ToLocation(this TemperatureLocation a) => new(a.Id, a.Name, a.MinTemperatureLimitCelsius, a.MaxTemperatureLimitCelsius);
}
