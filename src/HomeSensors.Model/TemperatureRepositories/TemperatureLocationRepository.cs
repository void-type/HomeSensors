using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.TemperatureRepositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Responses.Collections;

namespace HomeSensors.Model.TemperatureRepositories;

public class TemperatureLocationRepository
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
            .AsNoTracking()
            .ToListAsync();

        var results = new List<CheckLimitResult>();

        foreach (var location in locations)
        {
            var dbReadingsSince = _data.TemperatureReadings
                .AsNoTracking()
                .Where(x => x.Time >= lastCheck && x.TemperatureLocationId == location.Id);

            var minimum = await GetMinimumExceeded(location, dbReadingsSince);

            var maximum = await GetMaximumExceeded(location, dbReadingsSince);

            results.Add(new CheckLimitResult(location.ToLocation(), minimum, maximum));
        }

        return results;
    }

    private static async Task<Reading?> GetMinimumExceeded(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
    {
        if (!location.MinTemperatureLimit.HasValue)
        {
            return null;
        }

        var minimum = await dbReadingsSince
            .OrderBy(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync(x => x.TemperatureCelsius < location.MinTemperatureLimit);

        if (minimum is null)
        {
            return null;
        }

        return minimum.ToReading();
    }

    private static async Task<Reading?> GetMaximumExceeded(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
    {
        if (!location.MaxTemperatureLimit.HasValue)
        {
            return null;
        }

        var maximum = await dbReadingsSince
            .OrderByDescending(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync(x => x.TemperatureCelsius > location.MaxTemperatureLimit);

        if (maximum is null)
        {
            return null;
        }

        return maximum.ToReading();
    }
}
