using HomeSensors.Data.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Data.Repositories;

public class TemperatureLocationRepository
{
    private readonly HomeSensorsContext _data;

    public TemperatureLocationRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    public Task<List<TemperatureLocation>> GetAll(PaginationOptions paginationOptions)
    {
        return _data.TemperatureLocations
             .AsNoTracking()
             .OrderBy(x => x.Name)
             .GetPage(paginationOptions)
             .ToListAsync();
    }

    public async Task<List<CheckLimitResult>> CheckLimits(DateTimeOffset lastCheck)
    {
        var locations = await _data.TemperatureLocations
            .AsNoTracking()
            .ToListAsync();

        var results = new List<CheckLimitResult>();

        foreach (var location in locations)
        {
            var dbReadingsSince = _data.TemperatureReadings
                .AsNoTracking()
                .Where(x => x.Time >= lastCheck && x.TemperatureLocationId == location.Id);

            var minimum = await GetMinimum(location, dbReadingsSince);

            var maximum = await GetMaximum(location, dbReadingsSince);

            results.Add(new CheckLimitResult(location, minimum, maximum));
        }

        return results;
    }

    private static async Task<TemperatureReading?> GetMinimum(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
    {
        if (!location.MinTemperatureLimit.HasValue)
        {
            return null;
        }

        return await dbReadingsSince
            .OrderBy(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync(x => x.TemperatureCelsius < location.MinTemperatureLimit);
    }

    private static async Task<TemperatureReading?> GetMaximum(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
    {
        if (!location.MaxTemperatureLimit.HasValue)
        {
            return null;
        }

        return await dbReadingsSince
            .OrderByDescending(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync(x => x.TemperatureCelsius > location.MaxTemperatureLimit);
    }
}
