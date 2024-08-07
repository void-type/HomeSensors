﻿using HomeSensors.Model.Data.Models;
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
    public static List<TemperatureTimeSeriesPoint> GetIntervalAverages(this IEnumerable<TemperatureReading> readingsForLocation, int intervalMinutes)
    {
        if (intervalMinutes == 0)
        {
            return readingsForLocation
                .Select(reading => new TemperatureTimeSeriesPoint
                (
                    reading.Time,
                    reading.TemperatureCelsius,
                    reading.Humidity
                ))
                .ToList();
        }

        return readingsForLocation
            .GroupBy(r => r.Time.RoundDownMinutes(intervalMinutes))
            .Select(timeGroup => new TemperatureTimeSeriesPoint(timeGroup.Key, timeGroup.Average(s => s.TemperatureCelsius), timeGroup.Average(s => s.Humidity)))
            .ToList();
    }

    public static string FormatTemp(double tempCelsius, bool useFahrenheit = false)
    {
        var decimals = useFahrenheit ? 0 : 1;
        var convertedTemp = useFahrenheit ? (tempCelsius * 1.8) + 32 : tempCelsius;
        var num = Math.Round(convertedTemp, decimals, MidpointRounding.AwayFromZero);

        var unit = useFahrenheit ? "°F" : "°C";

        return $"{num}{unit}";
    }

    public static string GetDualTempString(double? tempCelsius)
    {
        if (!tempCelsius.HasValue)
        {
            return "null";
        }

        return $"{FormatTemp(tempCelsius.Value, true)} / {FormatTemp(tempCelsius.Value)}";
    }

    public static TemperatureReadingResponse ToApiResponse(this TemperatureReading a) => new(a.Time, a.Humidity, a.TemperatureCelsius, a.TemperatureLocation?.ToApiResponse());

    public static TemperatureLocationResponse ToApiResponse(this TemperatureLocation a) => new(a.Id, a.Name, a.MinTemperatureLimitCelsius, a.MaxTemperatureLimitCelsius);
}
