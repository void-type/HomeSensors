using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureReadingRepository
{
    private readonly HomeSensorsContext _data;
    private readonly IDateTimeService _dateTimeService;

    public TemperatureReadingRepository(HomeSensorsContext data, IDateTimeService dateTimeService)
    {
        _data = data;
        _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// Gets the latest reading from each location. Limited to locations that have readings within the last 24 hours.
    /// </summary>
    public async Task<List<CurrentReading>> GetCurrent()
    {
        var data = await _data.TemperatureLocations
            .AsNoTracking()
            .Include(x => x.TemperatureReadings)
            .OrderBy(x => x.Name != "Outside")
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                Location = x,
                Reading = x.TemperatureReadings.OrderByDescending(x => x.Time).FirstOrDefault(),
            })
            .Where(x => x.Reading != null && x.Reading.Time >= _dateTimeService.MomentWithOffset.AddDays(-1))
            .ToListAsync();

        return data.ConvertAll(x => new CurrentReading(
            location: x.Location.ToLocation(),
            temperatureCelsius: x.Reading!.TemperatureCelsius,
            humidity: x.Reading!.Humidity,
            time: x.Reading.Time
        ));
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">GraphTimeSeriesRequest</param>
    public async Task<List<GraphTimeSeries>> GetTimeSeries(GraphTimeSeriesRequest request)
    {
        if (request.LocationIds.Count == 0)
        {
            return new();
        }

        var dbReadings = await _data.TemperatureReadings
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId != null)
            .Where(x => request.LocationIds.Contains(x.TemperatureLocationId))
            .Where(x => x.Time >= request.StartTime && x.Time <= request.EndTime)
            .OrderByDescending(x => x.Time)
            .ToListAsync();

        if (dbReadings.Count == 0)
        {
            return new();
        }

        // Depending on the total span of data returned, we will create averages over intervals to prevent overloading the client with too many data points.
        var dbSpanHours = (dbReadings.Max(x => x.Time) - dbReadings.Min(x => x.Time)).TotalHours;

        var intervalMinutes = dbSpanHours switch
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
            .ConvertAll(readingsForLocation =>
            {
                var avgGraphPoints = readingsForLocation.GetIntervalAverages(intervalMinutes);

                var allValues = readingsForLocation.Select(x => x.TemperatureCelsius);

                return new GraphTimeSeries(
                    location: readingsForLocation.First().TemperatureLocation!.ToLocation(),
                    min: allValues.Min(),
                    max: allValues.Max(),
                    average: allValues.Average(),
                    points: avgGraphPoints
                );
            });
    }
}
