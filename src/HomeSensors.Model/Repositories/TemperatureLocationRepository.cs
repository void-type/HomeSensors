using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

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
    public async Task<List<TemperatureLocationResponse>> GetAll()
    {
        return (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync())
            .ConvertAll(x => x.ToApiResponse());
    }

    /// <summary>
    /// Check all locations for readings that exceed min/max since the last check.
    /// </summary>
    /// <param name="lastCheck">The time of the last check.</param>
    /// <returns>List of results</returns>
    public async Task<List<TemperatureCheckLimitResponse>> CheckLimits(DateTimeOffset lastCheck)
    {
        var locations = (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .ToListAsync())
            .Select(x => x.ToApiResponse());

        var results = new List<TemperatureCheckLimitResponse>();

        foreach (var location in locations)
        {
            var min = await GetMinExceeded(location, lastCheck);
            var max = await GetMaxExceeded(location, lastCheck);

            results.Add(new TemperatureCheckLimitResponse(location, min, max));
        }

        return results;
    }

    public async Task<IResult<EntityMessage<long>>> Save(TemperatureLocationSaveRequest request)
    {
        var failures = new List<IFailure>();

        if (request.Name.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Name is required.", "name"));
        }

        var nameUsedByAnother = await _data.TemperatureLocations
            .AnyAsync(x => x.Name == request.Name && x.Id != request.Id);

        if (nameUsedByAnother)
        {
            failures.Add(new Failure("Name already exists.", "name"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var location = await _data.TemperatureLocations
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (location is null)
        {
            location = new TemperatureLocation();
            _data.TemperatureLocations.Add(location);
        }

        location.Name = request.Name;
        location.MinTemperatureLimitCelsius = request.MinTemperatureLimitCelsius;
        location.MaxTemperatureLimitCelsius = request.MaxTemperatureLimitCelsius;

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Location saved.", location.Id));
    }

    public async Task<IResult<EntityMessage<long>>> Delete(int id)
    {
        var location = await _data.TemperatureLocations
            .FirstOrDefaultAsync(x => x.Id == id);

        if (location is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Location not found."));
        }

        var anyLocationReadings = await _data.TemperatureReadings
            .AnyAsync(x => x.TemperatureLocationId == id);

        if (anyLocationReadings)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Location has readings and cannot be deleted."));
        }

        var anyLocationDevices = await _data.TemperatureDevices
            .AnyAsync(x => x.TemperatureLocationId == id);

        if (anyLocationDevices)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Location is related to a device. Change the device's location before deleting the location."));
        }

        _data.TemperatureLocations.Remove(location);

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Location deleted.", location.Id));
    }

    private async Task<TemperatureReadingResponse?> GetMinExceeded(TemperatureLocationResponse location, DateTimeOffset lastCheck)
    {
        if (!location.MinTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        var min = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Where(x => x.Time >= lastCheck && x.TemperatureLocationId == location.Id && x.TemperatureCelsius < location.MinTemperatureLimitCelsius)
            .OrderBy(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync();

        return min?.ToApiResponse();
    }

    private async Task<TemperatureReadingResponse?> GetMaxExceeded(TemperatureLocationResponse location, DateTimeOffset lastCheck)
    {
        if (!location.MaxTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        var max = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Where(x => x.Time >= lastCheck && x.TemperatureLocationId == location.Id && x.TemperatureCelsius > location.MaxTemperatureLimitCelsius)
            .OrderByDescending(x => x.TemperatureCelsius)
            .FirstOrDefaultAsync();

        return max?.ToApiResponse();
    }
}
