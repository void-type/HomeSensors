﻿using HomeSensors.Model.Data.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Data;

public class TemperatureDeviceRepository
{
    private readonly HomeSensorsContext _data;
    private readonly IDateTimeService _dateTimeService;

    public TemperatureDeviceRepository(HomeSensorsContext data, IDateTimeService dateTimeService)
    {
        _data = data;
        _dateTimeService = dateTimeService;
    }

    public Task<List<TemperatureDevice>> GetAll(PaginationOptions paginationOptions)
    {
        return _data.TemperatureDevices
            .AsNoTracking()
            .OrderBy(x => x.DeviceModel)
            .ThenBy(x => x.DeviceId)
            .ThenBy(x => x.DeviceChannel)
            .GetPage(paginationOptions)
            .ToListAsync();
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
                LocationName = x.CurrentTemperatureLocation!.Name,
                LastReading = x.TemperatureReadings.OrderByDescending(x => x.Time).FirstOrDefault()
            })
            .Where(x => (x.LastReading == null || x.LastReading.Time < _dateTimeService.MomentWithOffset.AddHours(-2)) && !x.IsRetired)
            .ToListAsync();

        return data.ConvertAll(x => new InactiveDevice
        {
            Id = x.Id,
            DeviceModel = x.DeviceModel,
            DeviceId = x.DeviceId,
            DeviceChannel = x.DeviceChannel,
            LocationName = x.LocationName,
            LastReadingTemperatureCelsius = x.LastReading?.TemperatureCelsius,
            LastReadingTime = x.LastReading?.Time,
        });
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
        {
            Id = x.Id,
            DeviceModel = x.DeviceModel,
            DeviceId = x.DeviceId,
            DeviceChannel = x.DeviceChannel,
            LastReadingTemperatureCelsius = x.LastReading?.TemperatureCelsius,
            LastReadingTime = x.LastReading?.Time,
        });
    }
}