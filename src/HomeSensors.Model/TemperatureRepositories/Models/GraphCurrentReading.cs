namespace HomeSensors.Model.TemperatureRepositories.Models;

public class GraphCurrentReading
{
    public GraphCurrentReading(string location, double? temperatureCelsius, double? humidity, DateTimeOffset time)
    {
        Location = location;
        TemperatureCelsius = temperatureCelsius;
        Humidity = humidity;
        Time = time;
    }

    public string Location { get; }
    public double? TemperatureCelsius { get; }
    public double? Humidity { get; }
    public DateTimeOffset Time { get; }
}
