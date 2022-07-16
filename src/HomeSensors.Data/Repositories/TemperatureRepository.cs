using HomeSensors.Data.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeSensors.Data.Repositories;

public class TemperatureRepository
{
    private readonly HomeSensorsContext _data;

    public TemperatureRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    public async Task<List<GraphTimeSeries>> GetTemperatureTimeSeries(DateTimeOffset startTime, DateTimeOffset endTime, int intervalMinutes = 15)
    {
        // TODO: Fahrenheit selector (in ui)
        // TODO: SignalR / API with date selectors and granularity selector
        // Make live readings prettier
        // var startTime = DateTimeOffset.Now.AddHours(-48);
        // var endTime = DateTimeOffset.Now;

        // If too many points requested, force hourly.
        var wideRequest = endTime - startTime > TimeSpan.FromDays(3);
        intervalMinutes = wideRequest ? 60 : intervalMinutes;

        var data = await _data.TemperatureReadings
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId != null)
            .Where(x => x.Time > startTime && x.Time < endTime)
            .OrderByDescending(x => x.Time)
            .ToListAsync();

        return data
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(x => x.Key)
            .ToList()
            .ConvertAll(locationGroup =>
            {
                var groups = locationGroup
                    .GroupBy(y =>
                    {
                        // Round down to 30 minute intervals, zero milliseconds and seconds to make period-starting groups.
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

                return new GraphTimeSeries(locationGroup.Key, groups);
            });
    }

    public async Task<List<GraphCurrentReading>> GetCurrentReadings()
    {
        var data = await _data.TemperatureLocations
            .Include(x => x.TemperatureReadings)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                Location = x.Name,
                Readings = x.TemperatureReadings.Take(1),
            })
            .ToListAsync();

        return data
            .Where(x => x.Readings.Any())
            .Select(x => {
                var reading = x.Readings.First();
                return new GraphCurrentReading(x.Location, reading.TemperatureCelsius, reading.Time);
            })
            .ToList();
    }
}
