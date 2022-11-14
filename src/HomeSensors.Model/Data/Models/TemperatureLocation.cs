using Microsoft.EntityFrameworkCore;

namespace HomeSensors.Model.Data.Models;

[Index(nameof(Name), IsUnique = true)]
public class TemperatureLocation
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? MinTemperatureLimit { get; set; }
    public double? MaxTemperatureLimit { get; set; }

    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = new();
}
