using Microsoft.EntityFrameworkCore;

namespace HomeSensors.Data;

[Index(nameof(Name), IsUnique = true)]
public class TemperatureLocation
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual List<TemperatureReading> TemperatureReadings { get; set; } = new();
}
