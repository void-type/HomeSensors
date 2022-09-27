using HomeSensors.Data.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeSensors.Data.Repositories;

public class TemperatureLocationRepository
{
    private readonly HomeSensorsContext _data;

    public TemperatureLocationRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    public async Task<List<LimitCheckResult>> CheckLimits(DateTimeOffset lastCheck)
    {
        var locations = await _data.TemperatureLocations.ToListAsync();

        var results = new List<LimitCheckResult>();

        foreach (var location in locations)
        {
            var dbReadingsSince = _data.TemperatureReadings
                .Where(x => x.Time >= lastCheck && x.TemperatureLocationId == location.Id);

            TemperatureReading? minimum = null;

            if (location.MinTemperatureLimit.HasValue)
            {
                minimum = await dbReadingsSince
                    .OrderBy(x => x.TemperatureCelsius)
                    .FirstOrDefaultAsync(x => x.TemperatureCelsius < location.MinTemperatureLimit);
            }

            TemperatureReading? maximum = null;

            if (location.MaxTemperatureLimit.HasValue)
            {
                maximum = await dbReadingsSince
                    .OrderByDescending(x => x.TemperatureCelsius)
                    .FirstOrDefaultAsync(x => x.TemperatureCelsius > location.MaxTemperatureLimit);
            }

            results.Add(new LimitCheckResult(location, minimum, maximum));
        }

        return results;
    }
}
