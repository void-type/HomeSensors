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

public class TemperatureLocationRepository(HomeSensorsContext data, HybridCache cache, ITemperatureHubNotifier hubNotifier) : RepositoryBase
{

    /// <summary>
    /// Get all locations.
    /// </summary>
    public async Task<List<TemperatureLocationResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return (await data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.IsHidden)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken))
            .ConvertAll(x => x.ToApiResponse());
    }

    /// <summary>
    /// Check all locations for readings that exceed min/max since the last check.
    /// </summary>
    /// <param name="since">The time of the last check</param>
    /// <param name="isAveragingEnabled">If true, the check will average readings over the look back period</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of results</returns>
    public async Task<List<TemperatureCheckLimitResponse>> CheckLimitsAsync(DateTimeOffset since, bool isAveragingEnabled, CancellationToken cancellationToken)
    {
        var locations = (await data.TemperatureLocations
            .TagWith(GetTag())
            .AsNoTracking()
            .ToListAsync(cancellationToken))
            .Select(x => x.ToApiResponse());

        var results = new List<TemperatureCheckLimitResponse>();

        foreach (var location in locations)
        {
            var min = await GetMinExceededAsync(location, since, isAveragingEnabled, cancellationToken);
            var max = await GetMaxExceededAsync(location, since, isAveragingEnabled, cancellationToken);

            results.Add(new TemperatureCheckLimitResponse(location, min, max));
        }

        return results;
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(TemperatureLocationSaveRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<IFailure>();

        if (request.Name.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Name is required.", "name"));
        }

        var nameUsedByAnother = await data.TemperatureLocations
            .AnyAsync(x => x.Name == request.Name && x.Id != request.Id, cancellationToken);

        if (nameUsedByAnother)
        {
            failures.Add(new Failure("Name already exists.", "name"));
        }

        if (request.CategoryId is not null)
        {
            var category = await data.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken);

            if (category is null)
            {
                failures.Add(new Failure("Category not found.", "categoryId"));
            }
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var location = await data.TemperatureLocations
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (location is null)
        {
            location = new TemperatureLocation();
            await data.TemperatureLocations.AddAsync(location, cancellationToken);
        }

        location.Name = request.Name;
        location.MinTemperatureLimitCelsius = request.MinTemperatureLimitCelsius;
        location.MaxTemperatureLimitCelsius = request.MaxTemperatureLimitCelsius;
        location.IsHidden = request.IsHidden;
        location.Color = request.Color;
        location.CategoryId = request.CategoryId;

        await data.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(CacheHelpers.TemperatureLocationAllCacheTag, cancellationToken);
        await hubNotifier.NotifyCurrentReadingsChangedAsync(cancellationToken);

        return Result.Ok(EntityMessage.Create("Location saved.", location.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var location = await data.TemperatureLocations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (location is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Location not found."));
        }

        var anyLocationReadings = await data.TemperatureReadings
            .AnyAsync(x => x.TemperatureLocationId == id, cancellationToken);

        if (anyLocationReadings)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Location has readings and cannot be deleted."));
        }

        var anyLocationDevices = await data.TemperatureDevices
            .AnyAsync(x => x.TemperatureLocationId == id, cancellationToken);

        if (anyLocationDevices)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Location is related to a device. Change the device's location before deleting the location."));
        }

        data.TemperatureLocations.Remove(location);

        await data.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(CacheHelpers.TemperatureLocationAllCacheTag, cancellationToken);
        await hubNotifier.NotifyCurrentReadingsChangedAsync(cancellationToken);

        return Result.Ok(EntityMessage.Create("Location deleted.", location.Id));
    }

    private async Task<TemperatureReadingResponse?> GetMinExceededAsync(TemperatureLocationResponse location, DateTimeOffset since, bool isAveragingEnabled, CancellationToken cancellationToken)
    {
        if (!location.MinTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        if (!isAveragingEnabled)
        {
            var min = await data.TemperatureReadings
                .TagWith(GetTag())
                .AsNoTracking()
                .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id && x.TemperatureCelsius < location.MinTemperatureLimitCelsius)
                .OrderBy(x => x.TemperatureCelsius)
                .FirstOrDefaultAsync(cancellationToken);

            return min?.ToApiResponse();
        }

        // If averaging enabled, test average over the look back period.
        var lookBack = await data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id)
            .ToListAsync(cancellationToken);

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

    private async Task<TemperatureReadingResponse?> GetMaxExceededAsync(TemperatureLocationResponse location, DateTimeOffset since, bool isAveragingEnabled, CancellationToken cancellationToken)
    {
        if (!location.MaxTemperatureLimitCelsius.HasValue)
        {
            return null;
        }

        if (!isAveragingEnabled)
        {
            var max = await data.TemperatureReadings
                .TagWith(GetTag())
                .AsNoTracking()
                .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id && x.TemperatureCelsius > location.MaxTemperatureLimitCelsius)
                .OrderByDescending(x => x.TemperatureCelsius)
                .FirstOrDefaultAsync(cancellationToken);

            return max?.ToApiResponse();
        }

        // If averaging enabled, test average over the look back period.
        var lookBack = await data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .Where(x => x.Time >= since && x.TemperatureLocationId == location.Id)
            .ToListAsync(cancellationToken);

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
