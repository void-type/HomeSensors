-- Cleanup readings that weren't associated with a location. Cleanup devices that have no readings.

DELETE FROM TemperatureReadings WHERE TemperatureLocationId IS NULL

DELETE d FROM [HomeSensors].[dbo].[TemperatureDevices] AS d
WHERE NOT EXISTS ( SELECT *
FROM TemperatureReadings r
WHERE r.TemperatureDeviceId = d.Id  )


-- Clone data down from prod to test
TRUNCATE TABLE HomeSensorsTest.dbo.TemperatureReadings
DELETE FROM HomeSensorsTest.dbo.TemperatureDevices
DELETE FROM HomeSensorsTest.dbo.TemperatureLocations


SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocations ON
GO

INSERT INTO HomeSensorsTest.dbo.TemperatureLocations
  ([Id]
  ,[Name]
  ,[MaxTemperatureLimitCelsius]
  ,[MinTemperatureLimitCelsius])
SELECT *
FROM HomeSensors.dbo.TemperatureLocations

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocations OFF
GO


SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevices ON
GO

INSERT INTO HomeSensorsTest.dbo.TemperatureDevices
  ([Id]
  ,[DeviceModel]
  ,[DeviceId]
  ,[DeviceChannel]
  ,[CurrentTemperatureLocationId]
  ,[IsRetired])
SELECT *
FROM HomeSensors.dbo.TemperatureDevices

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevices OFF
GO
