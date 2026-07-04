using HomeSensors.Model.CameraSnapshots.Entities;
using HomeSensors.Model.CameraSnapshots.Helpers;
using HomeSensors.Model.CameraSnapshots.Models;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.CameraSnapshots.Repositories;

public class CameraSnapshotRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public CameraSnapshotRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    /// <summary>
    /// Get all cameras.
    /// </summary>
    public async Task<List<CameraSnapshotResponse>> GetAllAsync()
    {
        return (await _data.CameraSnapshots
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.IsHidden)
            .ThenBy(x => x.Name)
            .ToListAsync())
            .ConvertAll(x => x.ToApiResponse());
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(CameraSnapshotSaveRequest request)
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

        var nameUsedByAnother = await _data.CameraSnapshots
            .AnyAsync(x => x.Name == request.Name && x.Id != request.Id);

        if (nameUsedByAnother)
        {
            failures.Add(new Failure("Name already exists.", "name"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var camera = await _data.CameraSnapshots
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (camera is null)
        {
            camera = new CameraSnapshot();
            _data.CameraSnapshots.Add(camera);
        }

        camera.Name = request.Name;
        camera.SnapshotsPath = request.SnapshotsPath;
        camera.IsHidden = request.IsHidden;

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Camera saved.", camera.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id)
    {
        var camera = await _data.CameraSnapshots
            .FirstOrDefaultAsync(x => x.Id == id);

        if (camera is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Camera not found."));
        }

        _data.CameraSnapshots.Remove(camera);

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Camera deleted.", camera.Id));
    }
}
