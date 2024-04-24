using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureDeviceRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;
    private readonly IDateTimeService _dateTimeService;

    public TemperatureDeviceRepository(HomeSensorsContext data, IDateTimeService dateTimeService)
    {
        _data = data;
        _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// Get all devices with statuses and last readings.
    /// </summary>
    public async Task<List<Device>> GetAll()
    {
        var lastReadings = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .GroupBy(x => x.TemperatureDeviceId)
            .Select(g => g.OrderByDescending(x => x.Time).First())
            .ToListAsync();

        var data = (await _data.TemperatureDevices
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.IsRetired)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.DeviceModel,
                x.DeviceId,
                x.DeviceChannel,
                x.IsRetired,
                CurrentLocationId = x.CurrentTemperatureLocationId,
            })
            .ToListAsync())
            .ConvertAll(x => new
            {
                x.Id,
                x.DeviceModel,
                x.DeviceId,
                x.DeviceChannel,
                x.IsRetired,
                x.CurrentLocationId,
                LastReading = lastReadings.Find(r => r.TemperatureDeviceId == x.Id)
            })
;

        return data.ConvertAll(x => new Device
        (
            id: x.Id,
            deviceModel: x.DeviceModel,
            deviceId: x.DeviceId,
            deviceChannel: x.DeviceChannel,
            currentLocationId: x.CurrentLocationId,
            lastReading: x.LastReading?.ToReading(),
            isRetired: x.IsRetired,
            isLost: !x.IsRetired && x.CurrentLocationId is null,
            isInactive: !x.IsRetired && (x.LastReading is null || x.LastReading.Time < _dateTimeService.MomentWithOffset.AddMinutes(-20)),
            isBatteryLevelLow: !x.IsRetired && x.LastReading?.DeviceBatteryLevel is not null && x.LastReading?.DeviceBatteryLevel < 1
        ));
    }

    public async Task<IResult<EntityMessage<long>>> Update(DeviceUpdateRequest request)
    {
        if (request.CurrentLocationId is not null)
        {
            var locationExists = await _data.TemperatureLocations.AnyAsync(x => x.Id == request.CurrentLocationId);

            if (!locationExists)
            {
                return Result.Fail<EntityMessage<long>>(new Failure("Location doesn't exist.", "location"));
            }
        }

        return await _data.TemperatureDevices
            .FirstOrDefaultAsync(x => x.Id == request.Id)
            .MapAsync(x => Maybe.From(x))
            .ToResultAsync(new Failure("Device does not exist.", "id"))
            .TeeOnSuccessAsync(async x =>
            {
                x.IsRetired = request.IsRetired;
                x.CurrentTemperatureLocationId = request.CurrentLocationId;
                await _data.SaveChangesAsync();
            })
            .SelectAsync(x => EntityMessage.Create("Device saved.", x.Id));
    }
}
