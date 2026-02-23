-- Clone data down from prod to test

-- Reset any leftover IDENTITY_INSERT state from previous failed runs
SET IDENTITY_INSERT HomeSensorsTest.dbo.Category OFF
SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocation OFF
SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevice OFF
SET IDENTITY_INSERT HomeSensorsTest.dbo.HvacAction OFF
SET IDENTITY_INSERT HomeSensorsTest.dbo.WaterLeakDevice OFF
SET IDENTITY_INSERT HomeSensorsTest.dbo.EmailRecipient OFF
GO

TRUNCATE TABLE HomeSensorsTest.dbo.TemperatureReading
DELETE FROM HomeSensorsTest.dbo.HvacAction
DELETE FROM HomeSensorsTest.dbo.TemperatureDevice
DELETE FROM HomeSensorsTest.dbo.TemperatureLocation
DELETE FROM HomeSensorsTest.dbo.Category
DELETE FROM HomeSensorsTest.dbo.WaterLeakDevice
DELETE FROM HomeSensorsTest.dbo.EmailRecipient

SET IDENTITY_INSERT HomeSensorsTest.dbo.Category ON
GO

INSERT INTO HomeSensorsTest.dbo.Category
  ([Id], [Name], [Order])
SELECT
  [Id], [Name], [Order]
FROM
  HomeSensors.dbo.Category

SET IDENTITY_INSERT HomeSensorsTest.dbo.Category OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocation ON
GO

INSERT INTO HomeSensorsTest.dbo.TemperatureLocation
  ([Id], [Name], [MinTemperatureLimitCelsius], [MaxTemperatureLimitCelsius], [IsHidden], [Color], [CategoryId])
SELECT
  [Id], [Name], [MinTemperatureLimitCelsius], [MaxTemperatureLimitCelsius], [IsHidden], [Color], [CategoryId]
FROM
  HomeSensors.dbo.TemperatureLocation

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureLocation OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevice ON
GO

INSERT INTO HomeSensorsTest.dbo.TemperatureDevice
  ([Id], [Name], [MqttTopic], [IsRetired], [ExcludeFromInactiveAlerts], [InactiveLimitMinutes], [TemperatureLocationId])
SELECT
  [Id], [Name], [MqttTopic], [IsRetired], [ExcludeFromInactiveAlerts], [InactiveLimitMinutes], [TemperatureLocationId]
FROM
  HomeSensors.dbo.TemperatureDevice

SET IDENTITY_INSERT HomeSensorsTest.dbo.TemperatureDevice OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.HvacAction ON
GO

INSERT INTO HomeSensorsTest.dbo.HvacAction
  ([Id], [EntityId], [State], [LastChanged], [LastUpdated])
SELECT
  [Id], [EntityId], [State], [LastChanged], [LastUpdated]
FROM
  HomeSensors.dbo.HvacAction

SET IDENTITY_INSERT HomeSensorsTest.dbo.HvacAction OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.WaterLeakDevice ON
GO

INSERT INTO HomeSensorsTest.dbo.WaterLeakDevice
  ([Id], [Name], [MqttTopic])
SELECT
  [Id], [Name], [MqttTopic]
FROM
  HomeSensors.dbo.WaterLeakDevice

SET IDENTITY_INSERT HomeSensorsTest.dbo.WaterLeakDevice OFF
GO

SET IDENTITY_INSERT HomeSensorsTest.dbo.EmailRecipient ON
GO

INSERT INTO HomeSensorsTest.dbo.EmailRecipient
  ([Id], [Email])
SELECT
  [Id], [Email]
FROM
  HomeSensors.dbo.EmailRecipient

SET IDENTITY_INSERT HomeSensorsTest.dbo.EmailRecipient OFF
GO
