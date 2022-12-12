namespace HomeSensors.Model.TemperatureRepositories.Models;

public class Location
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double? MinTemperatureLimit { get; init; }
    public double? MaxTemperatureLimit { get; init; }
}
