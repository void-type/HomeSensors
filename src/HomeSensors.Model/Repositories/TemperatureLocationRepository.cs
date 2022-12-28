using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Model.Repositories;

public class TemperatureLocationRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public TemperatureLocationRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    /// <summary>
    /// Get all locations.
    /// </summary>
    /// <param name="paginationOptions"></param>
    public async Task<List<Location>> GetAll(PaginationOptions paginationOptions)
    {
        return (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name != "Outside")
            .ThenBy(x => x.Name)
            .GetPage(paginationOptions)
            .ToListAsync())
            .ConvertAll(x => x.ToLocation());
    }

    /// <summary>
    /// Check all locations for readings that exceed min/max since the last check.
    /// </summary>
    /// <param name="lastCheck">The time of the last check.</param>
    /// <returns>List of results</returns>
    public async Task<List<CheckLimitResult>> CheckLimits(DateTimeOffset lastCheck)
    {
        var locations = await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .ToListAsync();

        var results = new List<CheckLimitResult>();

        foreach (var location in locations)
        {
            var dbReadingsSince = _data.TemperatureReadings
                .TagWith(GetTag())
                .AsNoTracking()
                .Where(x => x.Time >= lastCheck && x.TemperatureLocationId == location.Id);

            var min = await GetMinExceeded(location, dbReadingsSince);

            var max = await GetMaxExceeded(location, dbReadingsSince);

            results.Add(new CheckLimitResult(location.ToLocation(), min, max));
        }

        return results;
    }

    private static async Task<CheckLimitResultReading?> GetMinExceeded(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
    {
        if (!location.MinTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        var min = await dbReadingsSince
            .OrderBy(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync(x => x.TemperatureCelsius < location.MinTemperatureLimitCelsius);

        if (min is null)
        {
            return null;
        }

        return min.ToCheckLimitResultReading();
    }

    private static async Task<CheckLimitResultReading?> GetMaxExceeded(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
    {
        if (!location.MaxTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        var max = await dbReadingsSince
            .OrderByDescending(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync(x => x.TemperatureCelsius > location.MaxTemperatureLimitCelsius);

        if (max is null)
        {
            return null;
        }

        return max.ToCheckLimitResultReading();
    }
}
