using HomeSensors.Model.Data;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
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
        var data = await _data.TemperatureDevices
            .TagWith(GetTag())
            .AsNoTracking()
            .Include(x => x.TemperatureReadings)
            .OrderBy(x => x.DeviceModel)
            .ThenBy(x => x.DeviceId)
            .ThenBy(x => x.DeviceChannel)
            .Select(x => new
            {
                x.Id,
                x.DeviceModel,
                x.DeviceId,
                x.DeviceChannel,
                x.IsRetired,
                CurrentLocationId = x.CurrentTemperatureLocationId,
                LastReading = x.TemperatureReadings.OrderByDescending(x => x.Time).FirstOrDefault()
            })
            .ToListAsync();

        return data.ConvertAll(x => new Device
        (
            id: x.Id,
            deviceModel: x.DeviceModel,
            deviceId: x.DeviceId,
            deviceChannel: x.DeviceChannel,
            currentLocationId: x.CurrentLocationId,
            lastReading: x.LastReading?.ToReading(),
            isRetired: x.IsRetired,
            isLost: x.CurrentLocationId is null && !x.IsRetired,
            isInactive: (x.LastReading is null || x.LastReading.Time < _dateTimeService.MomentWithOffset.AddHours(-2)) && !x.IsRetired
        ));
    }
}
