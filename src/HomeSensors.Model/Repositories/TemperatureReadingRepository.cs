using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureReadingRepository : RepositoryBase
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
    public async Task<List<TemperatureReadingResponse>> GetCurrent()
    {
        var data = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.Time >= _dateTimeService.MomentWithOffset.AddDays(-1))
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(g => g.Key != "Outside")
            .ThenBy(x => x.Key)
            .Select(g => g.OrderByDescending(x => x.Time).First())
            .ToListAsync();

        return data.ConvertAll(x => new TemperatureReadingResponse(
            time: x.Time,
            humidity: x.Humidity,
            temperatureCelsius: x.TemperatureCelsius,
            location: x.TemperatureLocation!.ToApiResponse()
        ));
    }

    /// <summary>
    /// Gets the latest reading for the location.
    /// </summary>
    public Task<Maybe<TemperatureReadingResponse>> GetCurrent(long locationId)
    {
        return _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId == locationId)
            .OrderByDescending(x => x.Time)
            .FirstOrDefaultAsync()
            .MapAsync(Maybe.From)
            .SelectAsync(x => new TemperatureReadingResponse(
                time: x.Time,
                humidity: x.Humidity,
                temperatureCelsius: x.TemperatureCelsius,
                location: x.TemperatureLocation!.ToApiResponse()));
    }

    /// <summary>
    /// Pull a time series of readings for multiple locations. Averages readings to reduce granularity at large scales.
    /// </summary>
    /// <param name="request">GraphTimeSeriesRequest</param>
    public async Task<List<TemperatureTimeSeriesResponse>> GetTimeSeries(TemperatureTimeSeriesRequest request)
    {
        if (request.LocationIds.Count == 0)
        {
            return [];
        }

        var dbReadings = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureLocation)
            .Where(x => request.LocationIds.Contains(x.TemperatureLocationId))
            .Where(x => x.Time >= request.StartTime && x.Time <= request.EndTime)
            .OrderByDescending(x => x.Time)
            .ToListAsync();

        if (dbReadings.Count == 0)
        {
            return [];
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

                var allTemps = readingsForLocation.Select(x => x.TemperatureCelsius);

                var tempAgg = new TemperatureTimeSeriesAggregate(
                    minimum: allTemps.Min(),
                    maximum: allTemps.Max(),
                    average: allTemps.Average());

                var allHumidity = readingsForLocation.Select(x => x.Humidity);

                var humidityAgg = new TemperatureTimeSeriesAggregate(
                    minimum: allHumidity.Min(),
                    maximum: allHumidity.Max(),
                    average: allHumidity.Average());

                return new TemperatureTimeSeriesResponse(
                    location: readingsForLocation.First().TemperatureLocation!.ToApiResponse(),
                    tempAgg,
                    humidityAgg,
                    points: avgGraphPoints
                );
            });
    }
}
