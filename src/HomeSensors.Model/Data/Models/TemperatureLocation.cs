namespace HomeSensors.Model.Data.Models;

public class TemperatureLocation
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? MinTemperatureLimitCelsius { get; set; }
    public double? MaxTemperatureLimitCelsius { get; set; }

    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = [];
}
