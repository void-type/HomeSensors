namespace HomeSensors.Model.Repositories.Models;

public class TimeSeriesAggregate
{
    public TimeSeriesAggregate(double? minimum, double? maximum, double? average)
    {
        Minimum = minimum;
        Maximum = maximum;
        Average = average;
    }

    public double? Minimum { get; }
    public double? Maximum { get; }
    public double? Average { get; }
}
