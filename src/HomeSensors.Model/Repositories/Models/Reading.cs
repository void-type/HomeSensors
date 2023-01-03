namespace HomeSensors.Model.Repositories.Models;

public class Reading
{
    public Reading(DateTimeOffset time, double? humidity, double? temperatureCelsius, Location? location)
    {
        Time = time;
        Humidity = humidity;
        TemperatureCelsius = temperatureCelsius;
        Location = location;
    }

    public DateTimeOffset Time { get; }
    public double? Humidity { get; }
    public double? TemperatureCelsius { get; }
    public Location? Location { get; }
}
