-- Cleanup readings that weren't associated with a location. Cleanup devices that have no readings.

DELETE FROM TemperatureReading WHERE TemperatureLocationId IS NULL

DELETE d FROM [HomeSensors].[dbo].[TemperatureDevice] AS d
WHERE NOT EXISTS ( SELECT *
FROM TemperatureReading r
WHERE r.TemperatureDeviceId = d.Id  )


-- Clone data down from prod to test
TRUNCATE TABLE HomeSensorsTest.dbo.TemperatureReading
DELETE FROM HomeSensorsTest.dbo.TemperatureDevice
DELETE FROM HomeSensorsTest.dbo.TemperatureLocation
DELETE FROM HomeSensorsTest.dbo.Category

SET IDENTITY_INSERT HomeSensorsTest.dbo.Category ON
GO

INSERT INTO HomeSensorsTest.dbo.Category
  ([Id]
  ,[Name]
  ,[Order])
SELECT
  *
FROM
  HomeSensors.dbo.Category

SET IDENTITY_INSERT HomeSensorsTest.dbo.Category OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocation ON
GO

INSERT INTO HomeSensorsTest.dbo.TemperatureLocation
  ([Id]
  ,[Name]
  ,[MaxTemperatureLimitCelsius]
  ,[MinTemperatureLimitCelsius]
  ,[CategoryId]
  ,[IsHidden])
SELECT
  *
FROM
  HomeSensors.dbo.TemperatureLocation

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocation OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevice ON
GO

INSERT INTO HomeSensorsTest.dbo.TemperatureDevice
  ([Id]
  ,[TemperatureLocationId]
  ,[IsRetired]
  ,[MqttTopic]
  ,[Name])
SELECT
  *
FROM
  HomeSensors.dbo.TemperatureDevice

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevice OFF
GO
