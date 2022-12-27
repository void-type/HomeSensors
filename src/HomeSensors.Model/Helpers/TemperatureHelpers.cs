using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Repositories.Models;

namespace HomeSensors.Model.Helpers;

public static class TemperatureHelpers
{
    /// <summary>
    /// Average a collection of readings into a series of regular time intervals.
    /// Interval keys are period starting. Eg: 8:05 to 8:09:59 with 5m intervals will key to 8:05.
    /// </summary>
    /// <param name="readingsForLocation">Readings to pull time series from</param>
    /// <param name="intervalMinutes">Interval length in minutes</param>
    public static List<GraphPoint> GetIntervalAverages(this IEnumerable<TemperatureReading> readingsForLocation, int intervalMinutes)
    {
        if (intervalMinutes == 0)
        {
            return readingsForLocation
                .Select(reading => new GraphPoint
                (
                    reading.Time,
                    reading.TemperatureCelsius
                ))
                .ToList();
        }

        return readingsForLocation
            .GroupBy(r => r.Time.RoundDownMinutes(intervalMinutes))
            .Select(timeGroup => new GraphPoint(timeGroup.Key, timeGroup.Average(s => s.TemperatureCelsius)))
            .ToList();
    }

    public static IQueryable<TemperatureReading> WhereShouldBeSummarized(this IQueryable<TemperatureReading> readings, DateTimeOffset cutoffLimit)
    {
        return readings.Where(x => !x.IsSummary && x.Time < cutoffLimit);
    }
}
