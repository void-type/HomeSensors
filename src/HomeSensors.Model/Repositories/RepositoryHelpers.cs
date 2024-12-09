using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories.Models;

namespace HomeSensors.Model.Repositories;

public static class RepositoryHelpers
{
    /// <summary>
    /// Average a collection of readings into a series of regular time intervals.
    /// Interval keys are period starting. Eg: 8:05 to 8:09:59 with 5m intervals will key to 8:05.
    /// </summary>
    /// <param name="readingsForLocation">Readings to pull time series from</param>
    /// <param name="intervalMinutes">Interval length in minutes</param>
    public static List<TemperatureTimeSeriesPoint> GetIntervalAverages(this IEnumerable<TemperatureReading> readingsForLocation, int intervalMinutes)
    {
        if (intervalMinutes == 0)
        {
            return readingsForLocation
                .Select(reading => new TemperatureTimeSeriesPoint
                (
                    time: reading.Time,
                    temperatureCelsius: reading.TemperatureCelsius,
                    humidity: reading.Humidity
                ))
                .ToList();
        }

        return readingsForLocation
            .GroupBy(r => r.Time.RoundDownMinutes(intervalMinutes))
            .Select(timeGroup => new TemperatureTimeSeriesPoint(timeGroup.Key, timeGroup.Average(s => s.TemperatureCelsius), timeGroup.Average(s => s.Humidity)))
            .ToList();
    }

    /// <summary>
    /// Average a collection of readings into a single point.
    /// </summary>
    /// <param name="readingsForLocation">Readings to pull time series from</param>
    public static TemperatureTimeSeriesPoint GetSetAverage(this IEnumerable<TemperatureReading> readingsForLocation)
    {
        var readings = readingsForLocation.ToList();

        return new TemperatureTimeSeriesPoint
        (
            time: readings.Min(r => r.Time),
            temperatureCelsius: readings.Average(r => r.TemperatureCelsius),
            humidity: readings.Average(r => r.Humidity)
        );
    }

    public static TemperatureReadingResponse ToApiResponse(this TemperatureReading a) =>
        new(
            time: a.Time,
            humidity: a.Humidity,
            temperatureCelsius: a.TemperatureCelsius,
            location: a.TemperatureLocation?.ToApiResponse());

    public static TemperatureLocationResponse ToApiResponse(this TemperatureLocation a) =>
        new(
            id: a.Id,
            name: a.Name,
            minTemperatureLimitCelsius: a.MinTemperatureLimitCelsius,
            maxTemperatureLimitCelsius: a.MaxTemperatureLimitCelsius,
            isHidden: a.IsHidden,
            categoryId: a.CategoryId);
}
