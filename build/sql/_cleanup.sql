-- Cleanup readings that weren't associated with a location. Cleanup devices that have no readings.

delete from TemperatureReadings where TemperatureLocationId is null

delete d FROM [HomeSensors].[dbo].[TemperatureDevices] as d
where not exists ( select * from TemperatureReadings r where r.TemperatureDeviceId = d.Id  )
