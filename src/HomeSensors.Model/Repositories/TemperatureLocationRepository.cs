using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;

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
    public async Task<List<Location>> GetAll()
    {
        return (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name != "Outside")
            .ThenBy(x => x.Name)
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
        var locations = (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .ToListAsync())
            .Select(x => x.ToLocation());

        var results = new List<CheckLimitResult>();

        foreach (var location in locations)
        {
            var min = await GetMinExceeded(location, lastCheck);
            var max = await GetMaxExceeded(location, lastCheck);

            results.Add(new CheckLimitResult(location, min, max));
        }

        return results;
    }

    public Task<IResult<EntityMessage<long>>> Create(CreateLocationRequest request)
    {
        return ValidateName(request.Name)
            .ThenAsync(() => ValidateNameIsAvailableAsync(request.Name))
            .SelectAsync(async () =>
            {
                var newLocation = new TemperatureLocation()
                {
                    Name = request.Name,
                    MinTemperatureLimitCelsius = request.MinTemperatureLimitCelsius,
                    MaxTemperatureLimitCelsius = request.MaxTemperatureLimitCelsius,
                };

                var savedLocationEntity = _data.TemperatureLocations.Add(newLocation);

                await _data.SaveChangesAsync();

                return savedLocationEntity;
            })
            .SelectAsync(x => EntityMessage.Create("Location added.", x.Entity.Id));
    }

    public Task<IResult<EntityMessage<long>>> Update(UpdateLocationRequest request)
    {
        return ValidateName(request.Name)
            .ThenAsync(async () =>
            {
                return (await _data.TemperatureLocations.FirstOrDefaultAsync(x => x.Id == request.Id))
                    .Map(x => Maybe.From(x))
                    .ToResult(new Failure("Location does not exist.", "id"));
            })
            .TeeOnSuccessAsync(async x =>
            {
                x.Name = request.Name;
                x.MinTemperatureLimitCelsius = request.MinTemperatureLimitCelsius;
                x.MaxTemperatureLimitCelsius = request.MaxTemperatureLimitCelsius;
                await _data.SaveChangesAsync();
            })
            .SelectAsync(x => EntityMessage.Create("Location saved.", x.Id));
    }

    private static IResult ValidateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Fail(new Failure("Name is required.", "name"));
        }

        return Result.Ok();
    }

    private async Task<IResult> ValidateNameIsAvailableAsync(string newName)
    {
        var nameExists = await _data.TemperatureLocations.AnyAsync(x => x.Name == newName);

        if (nameExists)
        {
            return Result.Fail(new Failure("Name already exists.", "name"));
        }

        return Result.Ok();
    }

    private async Task<Reading?> GetMinExceeded(Location location, DateTimeOffset lastCheck)
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

        return min?.ToReading();
    }

    private async Task<Reading?> GetMaxExceeded(Location location, DateTimeOffset lastCheck)
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

        return max?.ToReading();
    }
}
