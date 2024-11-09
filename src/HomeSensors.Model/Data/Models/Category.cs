namespace HomeSensors.Model.Data.Models;

public class Category
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public virtual List<TemperatureLocation> TemperatureLocations { get; } = [];
}
