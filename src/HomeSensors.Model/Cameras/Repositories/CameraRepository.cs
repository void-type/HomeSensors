using HomeSensors.Model.Cameras.Entities;
using HomeSensors.Model.Cameras.Helpers;
using HomeSensors.Model.Cameras.Models;
using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.Cameras.Repositories;

public class CameraRepository(HomeSensorsContext data, HybridCache cache) : RepositoryBase
{

    /// <summary>
    /// Get all cameras.
    /// </summary>
    public async Task<List<CameraResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            GetCaller(),
            async cancel => (await data.Cameras
                .TagWith(GetTag())
                .AsNoTracking()
                .OrderBy(x => x.IsHidden)
                .ThenBy(x => x.Name)
                .ToListAsync(cancel))
                .ConvertAll(x => x.ToApiResponse()),
            tags: [CacheHelpers.CameraAllCacheTag],
            cancellationToken: cancellationToken);
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(CameraSaveRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<IFailure>();

        if (request.Name.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Name is required.", "name"));
        }

        if (request.SnapshotsPath.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Snapshots path is required.", "snapshotsPath"));
        }

        var nameUsedByAnother = await data.Cameras
            .AnyAsync(x => x.Name == request.Name && x.Id != request.Id, cancellationToken);

        if (nameUsedByAnother)
        {
            failures.Add(new Failure("Name already exists.", "name"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var camera = await data.Cameras
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (camera is null)
        {
            camera = new Camera();
            await data.Cameras.AddAsync(camera, cancellationToken);
        }

        camera.Name = request.Name;
        camera.SnapshotsPath = request.SnapshotsPath;
        camera.IsHidden = request.IsHidden;

        await data.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(CacheHelpers.CameraAllCacheTag, cancellationToken);

        return Result.Ok(EntityMessage.Create("Camera saved.", camera.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var camera = await data.Cameras
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (camera is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Camera not found."));
        }

        data.Cameras.Remove(camera);

        await data.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(CacheHelpers.CameraAllCacheTag, cancellationToken);

        return Result.Ok(EntityMessage.Create("Camera deleted.", camera.Id));
    }
}
