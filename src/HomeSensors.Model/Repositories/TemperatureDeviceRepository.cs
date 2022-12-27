using HomeSensors.Model.Data;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Repositories;

public class TemperatureDeviceRepository
{
    private readonly HomeSensorsContext _data;
    private readonly IDateTimeService _dateTimeService;

    public TemperatureDeviceRepository(HomeSensorsContext data, IDateTimeService dateTimeService)
    {
        _data = data;
        _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// Devices that haven't saved any data in 2 hours.
    /// </summary>
    public async Task<List<InactiveDevice>> GetInactive()
    {
        var data = await _data.TemperatureDevices
            .AsNoTracking()
            .Include(x => x.CurrentTemperatureLocation)
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
                Location = x.CurrentTemperatureLocation!,
                LastReading = x.TemperatureReadings.OrderByDescending(x => x.Time).FirstOrDefault()
            })
            .Where(x => (x.LastReading == null || x.LastReading.Time < _dateTimeService.MomentWithOffset.AddHours(-2)) && !x.IsRetired)
            .ToListAsync();

        return data.ConvertAll(x => new InactiveDevice
        (
            id: x.Id,
            deviceModel: x.DeviceModel,
            deviceId: x.DeviceId,
            deviceChannel: x.DeviceChannel,
            location: x.Location.ToLocation(),
            lastReadingTemperatureCelsius: x.LastReading?.TemperatureCelsius,
            lastReadingTime: x.LastReading?.Time
        ));
    }

    /// <summary>
    /// Devices that have no location.
    /// </summary>
    public async Task<List<LostDevice>> GetLost()
    {
        var data = await _data.TemperatureDevices
            .AsNoTracking()
            .Include(x => x.TemperatureReadings)
            .OrderBy(x => x.DeviceModel)
            .ThenBy(x => x.DeviceId)
            .ThenBy(x => x.DeviceChannel)
            .Where(x => x.CurrentTemperatureLocationId == null && !x.IsRetired)
            .Select(x => new
            {
                x.Id,
                x.DeviceModel,
                x.DeviceId,
                x.DeviceChannel,
                LastReading = x.TemperatureReadings.OrderByDescending(x => x.Time).FirstOrDefault()
            })
            .ToListAsync();

        return data.ConvertAll(x => new LostDevice
        (
            id: x.Id,
            deviceModel: x.DeviceModel,
            deviceId: x.DeviceId,
            deviceChannel: x.DeviceChannel,
            lastReadingTemperatureCelsius: x.LastReading?.TemperatureCelsius,
            lastReadingTime: x.LastReading?.Time
        ));
    }
}
