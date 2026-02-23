using HomeSensors.Model.Data;
using HomeSensors.Model.WaterLeak.Entities;
using HomeSensors.Model.WaterLeak.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.WaterLeak.Repositories;

public class WaterLeakDeviceRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public WaterLeakDeviceRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    public async Task<List<WaterLeakDeviceResponse>> GetAllAsync()
    {
        return await _data.WaterLeakDevices
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WaterLeakDeviceResponse(x.Id, x.Name, x.MqttTopic, x.InactiveLimitMinutes))
            .ToListAsync();
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(WaterLeakDeviceSaveRequest request)
    {
        var failures = new List<IFailure>();

        if (request.Name.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Device requires a name.", "name"));
        }

        if (request.MqttTopic.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Device requires an MQTT Topic.", "mqttTopic"));
        }

        if (request.InactiveLimitMinutes < 0)
        {
            failures.Add(new Failure("Inactive limit must be 0 or greater.", "inactiveLimitMinutes"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var device = await _data.WaterLeakDevices
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (device is null)
        {
            device = new WaterLeakDevice();
            _data.WaterLeakDevices.Add(device);
        }

        device.Name = request.Name;
        device.MqttTopic = request.MqttTopic;
        device.InactiveLimitMinutes = request.InactiveLimitMinutes;

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Device saved.", device.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id)
    {
        var device = await _data.WaterLeakDevices
            .FirstOrDefaultAsync(x => x.Id == id);

        if (device is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Device not found."));
        }

        _data.WaterLeakDevices.Remove(device);

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Device deleted.", device.Id));
    }
}
