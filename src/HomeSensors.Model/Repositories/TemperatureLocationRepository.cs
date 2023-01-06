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

    public Task<IResult<EntityMessage<long>>> Create(CreateLocationRequest request)
    {
        return ValidateName(request.Name)
            .ThenAsync(() => ValidateNameIsAvailableAsync(request.Name))
            .SelectAsync(async () =>
            {
                var newLocation = new TemperatureLocation()
                {
                    Name = request.Name,
                    MinTemperatureLimitCelsius = request.MinLimitTemperatureCelsius,
                    MaxTemperatureLimitCelsius = request.MaxLimitTemperatureCelsius,
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
            .TeeOnSuccessAsync(x =>
            {
                x.Name = request.Name;
                x.MinTemperatureLimitCelsius = request.MinLimitTemperatureCelsius;
                x.MaxTemperatureLimitCelsius = request.MaxLimitTemperatureCelsius;
                _data.SaveChanges();
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

    private static async Task<Reading?> GetMinExceeded(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
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

        return min.ToReading();
    }

    private static async Task<Reading?> GetMaxExceeded(TemperatureLocation location, IQueryable<TemperatureReading> dbReadingsSince)
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

        return max.ToReading();
    }
}
