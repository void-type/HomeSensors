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

    public async Task<List<WaterLeakDeviceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _data.WaterLeakDevices
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WaterLeakDeviceResponse(x.Id, x.Name, x.MqttTopic, x.InactiveLimitMinutes))
            .ToListAsync(cancellationToken);
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(WaterLeakDeviceSaveRequest request, CancellationToken cancellationToken)
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
        else
        {
            var topicAlreadyUsed = await _data.WaterLeakDevices
                .AnyAsync(x => x.MqttTopic == request.MqttTopic && x.Id != request.Id, cancellationToken);

            if (topicAlreadyUsed)
            {
                failures.Add(new Failure("MQTT topic is already used by another device.", "mqttTopic"));
            }
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
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (device is null)
        {
            device = new WaterLeakDevice();
            await _data.WaterLeakDevices.AddAsync(device, cancellationToken);
        }

        device.Name = request.Name;
        device.MqttTopic = request.MqttTopic;
        device.InactiveLimitMinutes = request.InactiveLimitMinutes;

        await _data.SaveChangesAsync(cancellationToken);

        return Result.Ok(EntityMessage.Create("Device saved.", device.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var device = await _data.WaterLeakDevices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (device is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Device not found."));
        }

        _data.WaterLeakDevices.Remove(device);

        await _data.SaveChangesAsync(cancellationToken);

        return Result.Ok(EntityMessage.Create("Device deleted.", device.Id));
    }
}
