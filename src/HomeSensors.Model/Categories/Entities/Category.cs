using HomeSensors.Model.Temperature.Entities;

namespace HomeSensors.Model.Categories.Entities;

public class Category
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public virtual List<TemperatureLocation> TemperatureLocations { get; } = [];
}
