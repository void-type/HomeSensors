namespace HomeSensors.Model.Repositories.Models;

public class Reading
{
    public Reading(DateTimeOffset time, double? humidity, double? temperatureCelsius, Location? location)
    {
        Time = time;
        Humidity = humidity;
        TemperatureCelsius = temperatureCelsius;
        Location = location;

        var minLimit = Location?.MinTemperatureLimitCelsius;
        var maxLimit = Location?.MaxTemperatureLimitCelsius;

        if (minLimit.HasValue && temperatureCelsius.HasValue && temperatureCelsius < minLimit)
        {
            IsCold = true;
        }

        if (maxLimit.HasValue && temperatureCelsius.HasValue && temperatureCelsius > maxLimit)
        {
            IsHot = true;
        }
    }

    public DateTimeOffset Time { get; }
    public double? Humidity { get; }
    public double? TemperatureCelsius { get; }
    public Location? Location { get; }
    public bool IsHot { get; }
    public bool IsCold { get; }
}
