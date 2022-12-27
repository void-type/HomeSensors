namespace HomeSensors.Model.Repositories.Models;

public class CurrentReading
{
    public CurrentReading(Location location, double? temperatureCelsius, double? humidity, DateTimeOffset time)
    {
        Location = location;
        TemperatureCelsius = temperatureCelsius;
        Humidity = humidity;
        Time = time;
    }

    public DateTimeOffset Time { get; }
    public double? TemperatureCelsius { get; }
    public double? Humidity { get; }
    public Location Location { get; }
}
