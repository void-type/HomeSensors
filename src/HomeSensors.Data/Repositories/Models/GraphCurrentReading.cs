namespace HomeSensors.Data.Repositories.Models;

public class GraphCurrentReading
{
    public string Location { get; }
    public double? TemperatureCelsius { get; }
    public DateTimeOffset Time { get; }

    public GraphCurrentReading(string location, double? temperatureCelsius, DateTimeOffset time)
    {
        Location = location;
        TemperatureCelsius = temperatureCelsius;
        Time = time;
    }
}
