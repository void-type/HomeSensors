using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Notifications;
using HomeSensors.Model.Temperature.Entities;
using HomeSensors.Model.Temperature.Helpers;
using HomeSensors.Model.Temperature.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.Temperature.Repositories;

public class TemperatureLocationRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;
    private readonly HybridCache _cache;
    private readonly ITemperatureHubNotifier _hubNotifier;

    public TemperatureLocationRepository(HomeSensorsContext data, HybridCache cache, ITemperatureHubNotifier hubNotifier)
    {
        _data = data;
        _cache = cache;
        _hubNotifier = hubNotifier;
    }

    /// <summary>
    /// Get all locations.
    /// </summary>
    public async Task<List<TemperatureLocationResponse>> GetAllAsync()
    {
        return (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.IsHidden)
            .ThenBy(x => x.Name)
            .ToListAsync())
            .ConvertAll(x => x.ToApiResponse());
    }

    /// <summary>
    /// Check all locations for readings that exceed min/max since the last check.
    /// </summary>
    /// <param name="since">The time of the last check</param>
    /// <param name="isAveragingEnabled">If true, the check will average readings over the look back period</param>
    /// <returns>List of results</returns>
    public async Task<List<TemperatureCheckLimitResponse>> CheckLimitsAsync(DateTimeOffset since, bool isAveragingEnabled)
    {
        var locations = (await _data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .ToListAsync())
            .Select(x => x.ToApiResponse());

        var results = new List<TemperatureCheckLimitResponse>();

        foreach (var location in locations)
        {
            var min = await GetMinExceededAsync(location, since, isAveragingEnabled);
            var max = await GetMaxExceededAsync(location, since, isAveragingEnabled);

            results.Add(new TemperatureCheckLimitResponse(location, min, max));
        }

        return results;
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(TemperatureLocationSaveRequest request)
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

        if (request.CategoryId is not null)
        {
            var category = await _data.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId);

            if (category is null)
            {
                failures.Add(new Failure("Category not found.", "categoryId"));
            }
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
        location.IsHidden = request.IsHidden;
        location.Color = request.Color;
        location.CategoryId = request.CategoryId;

        await _data.SaveChangesAsync();

        await _cache.RemoveByTagAsync(CacheHelpers.TemperatureLocationAllCacheTag);
        await _hubNotifier.NotifyCurrentReadingsChangedAsync();

        return Result.Ok(EntityMessage.Create("Location saved.", location.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(int id)
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

        await _cache.RemoveByTagAsync(CacheHelpers.TemperatureLocationAllCacheTag);
        await _hubNotifier.NotifyCurrentReadingsChangedAsync();

        return Result.Ok(EntityMessage.Create("Location deleted.", location.Id));
    }

    private async Task<TemperatureReadingResponse?> GetMinExceededAsync(TemperatureLocationResponse location, DateTimeOffset since, bool isAveragingEnabled)
    {
        if (!location.MinTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        if (!isAveragingEnabled)
        {
            var min = await _data.TemperatureReadings
                .TagWith(GetTag())
                .AsNoTracking()
                .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id && x.TemperatureCelsius < location.MinTemperatureLimitCelsius)
                .OrderBy(x => x.TemperatureCelsius)
                .FirstOrDefaultAsync();

            return min?.ToApiResponse();
        }

        // If averaging enabled, test average over the look back period.
        var lookBack = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id)
            .ToListAsync();

        if (lookBack.Count == 0)
        {
            return null;
        }

        var lookBackAverage = lookBack.GetSetAverage();

        if (lookBackAverage.TemperatureCelsius < location.MinTemperatureLimitCelsius)
        {
            return new(
                time: lookBackAverage.Time,
                humidity: lookBackAverage.Humidity,
                temperatureCelsius: lookBackAverage.TemperatureCelsius,
                location: location);
        }

        return null;
    }

    private async Task<TemperatureReadingResponse?> GetMaxExceededAsync(TemperatureLocationResponse location, DateTimeOffset since, bool isAveragingEnabled)
    {
        if (!location.MaxTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        if (!isAveragingEnabled)
        {
            var max = await _data.TemperatureReadings
                .TagWith(GetTag())
                .AsNoTracking()
                .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id && x.TemperatureCelsius > location.MaxTemperatureLimitCelsius)
                .OrderByDescending(x => x.TemperatureCelsius)
                .FirstOrDefaultAsync();

            return max?.ToApiResponse();
        }

        // If averaging enabled, test average over the look back period.
        var lookBack = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id)
            .ToListAsync();

        if (lookBack.Count == 0)
        {
            return null;
        }

        var lookBackAverage = lookBack.GetSetAverage();

        if (lookBackAverage.TemperatureCelsius > location.MaxTemperatureLimitCelsius)
        {
            return new(
                time: lookBackAverage.Time,
                humidity: lookBackAverage.Humidity,
                temperatureCelsius: lookBackAverage.TemperatureCelsius,
                location: location);
        }

        return null;
    }
}
