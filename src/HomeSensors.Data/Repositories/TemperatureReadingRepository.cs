using HomeSensors.Data.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Time;

namespace HomeSensors.Data.Repositories;

public class TemperatureReadingRepository
{
    private readonly HomeSensorsContext _data;
    private readonly IDateTimeService _dateTimeService;

    public TemperatureReadingRepository(HomeSensorsContext data, IDateTimeService dateTimeService)
    {
        _data = data;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<GraphCurrentReading>> GetCurrentReadings()
    {
        var data = await _data.TemperatureLocations
            .Include(x => x.TemperatureReadings)
            .OrderBy(x => x.Name != "Outside")
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                Location = x.Name,
                Reading = x.TemperatureReadings.OrderByDescending(x => x.Time).FirstOrDefault(),
            })
            .Where(x => x.Reading != null && x.Reading.Time >= _dateTimeService.MomentWithOffset.AddDays(-1))
            .ToListAsync();

        return data
            .ConvertAll(x => new GraphCurrentReading(x.Location, x.Reading!.TemperatureCelsius, x.Reading!.Humidity, x.Reading.Time));
    }

    public async Task<List<GraphTimeSeries>> GetTimeSeries(GraphTimeSeriesRequest request)
    {
        var dbReadings = await _data.TemperatureReadings
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId != null)
            .Where(x => x.Time > request.StartTime && x.Time < request.EndTime)
            .OrderByDescending(x => x.Time)
            .ToListAsync();

        if (dbReadings.Count == 0)
        {
            return new();
        }

        // Depending on the total span of data returned, we will create averages over intervals to prevent overloading the client with too many data points.
        var dbSpan = (dbReadings.Max(x => x.Time) - dbReadings.Min(x => x.Time)).TotalHours;

        var intervalMinutes = dbSpan switch
        {
            > 3 * 24 => 60,
            > 2 * 24 => 30,
            > 1 * 24 => 15,
            > 12 => 10,
            > 6 => 5,
            > 3 => 1,
            _ => 0,
        };

        return dbReadings
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(x => x.Key != "Outside")
            .ThenBy(x => x.Key)
            .ToList()
            .ConvertAll(locationGroup =>
            {
                var points = GetReadingAveragesForIntervals(locationGroup, intervalMinutes);

                var values = locationGroup.Select(x => x.TemperatureCelsius);

                return new GraphTimeSeries(locationGroup.Key, values.Min(), values.Max(), values.Average(), points);
            });
    }

    private static List<GraphPoint> GetReadingAveragesForIntervals(IGrouping<string, TemperatureReading> locationGroup, int intervalMinutes)
    {
        if (intervalMinutes == 0)
        {
            return locationGroup
                .Select(reading => new GraphPoint
                {
                    Time = reading.Time,
                    TemperatureCelsius = reading.TemperatureCelsius
                })
                .ToList();
        }

        return locationGroup
            .GroupBy(y =>
            {
                var time = y.Time.AddMilliseconds(-y.Time.Millisecond - (1000 * y.Time.Second));
                return time.AddMinutes(-(time.Minute % intervalMinutes));
            })
            .Select(timeGroup =>
            {
                var intervalAverage = timeGroup.Average(s => s.TemperatureCelsius);

                return new GraphPoint
                {
                    Time = timeGroup.Key,
                    TemperatureCelsius = intervalAverage
                };
            })
            .ToList();
    }
}
