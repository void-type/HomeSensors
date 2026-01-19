using HomeSensors.Model.Data;
using HomeSensors.Model.Temperature.Entities;
using HomeSensors.Model.Temperature.Helpers;
using HomeSensors.Model.Temperature.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Temperature.Repositories;

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
    public async Task<List<TemperatureDeviceResponse>> GetAllAsync()
    {
        var lastReadings = await _data.TemperatureReadings
            .TagWith(GetTag())
            .AsNoTracking()
            .GroupBy(x => x.TemperatureDeviceId)
            .Select(g => g.OrderByDescending(x => x.Time).First())
            .ToListAsync();

        var data = await _data.TemperatureDevices
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.MqttTopic,
                x.IsRetired,
                x.ExcludeFromInactiveAlerts,
                LocationId = x.TemperatureLocationId,
            })
            .ToListAsync();

        return data.ConvertAll(x =>
        {
            var lastReading = lastReadings.Find(r => r.TemperatureDeviceId == x.Id);

            return new TemperatureDeviceResponse
            (
                id: x.Id,
                name: x.Name,
                mqttTopic: x.MqttTopic,
                locationId: x.LocationId,
                lastReading: lastReading?.ToApiResponse(),
                isRetired: x.IsRetired,
                isInactive: !x.IsRetired && (lastReading is null || lastReading.Time < _dateTimeService.MomentWithOffset.AddMinutes(-20)),
                isBatteryLevelLow: !x.IsRetired && lastReading?.DeviceBatteryLevel is not null && lastReading.DeviceBatteryLevel < 1,
                excludeFromInactiveAlerts: x.ExcludeFromInactiveAlerts
            );
        });
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(TemperatureDeviceSaveRequest request)
    {
        var failures = new List<IFailure>();

        if (!request.IsRetired)
        {
            var locationExists = await _data.TemperatureLocations.AnyAsync(x => x.Id == request.LocationId);

            if (!locationExists)
            {
                failures.Add(new Failure("Location doesn't exist.", "location"));
            }
        }

        if (request.Name.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Device requires a name.", "name"));
        }

        if (request.MqttTopic.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Device requires an MQTT Topic.", "mqttTopic"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var device = await _data.TemperatureDevices
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (device is null)
        {
            device = new TemperatureDevice();
            _data.TemperatureDevices.Add(device);
        }

        device.Name = request.Name;
        device.MqttTopic = request.MqttTopic;
        device.IsRetired = request.IsRetired;
        device.ExcludeFromInactiveAlerts = request.ExcludeFromInactiveAlerts;
        device.TemperatureLocationId = request.LocationId;

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Device saved.", device.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(int id)
    {
        var device = await _data.TemperatureDevices
            .FirstOrDefaultAsync(x => x.Id == id);

        if (device is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Device not found."));
        }

        _data.TemperatureDevices.Remove(device);

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Device deleted.", device.Id));
    }
}
