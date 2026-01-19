namespace HomeSensors.Model.Temperature.Models;

public class TemperatureTimeSeriesAggregate
{
    public TemperatureTimeSeriesAggregate(double? minimum, double? maximum, double? average)
    {
        Minimum = minimum;
        Maximum = maximum;
        Average = average;
    }

    public double? Minimum { get; }

    public double? Maximum { get; }

    public double? Average { get; }
}
