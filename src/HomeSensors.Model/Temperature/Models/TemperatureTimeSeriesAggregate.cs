namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesAggregate(double? minimum, double? maximum, double? average)
{
    public double? Minimum { get; } = minimum;

    public double? Maximum { get; } = maximum;

    public double? Average { get; } = average;
}
