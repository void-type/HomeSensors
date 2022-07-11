-- Run dotnet ef database update DeviceNormalization1
-- Run the following SQL
-- Run dotnet ef database update DeviceNormalization2

USE [HomeSensorsDev]
GO

INSERT INTO [dbo].[TemperatureLocations]
           ([Name])
     VALUES
           ('Garage')

INSERT INTO [dbo].[TemperatureLocations]
           ([Name])
     VALUES
           ('Bedroom')

INSERT INTO [dbo].[TemperatureLocations]
           ([Name])
     VALUES
           ('Garage Freezer')

INSERT INTO [dbo].[TemperatureLocations]
           ([Name])
     VALUES
           ('Garage Fridge')

INSERT INTO [dbo].[TemperatureDevices]
           ([DeviceModel]
           ,[DeviceId]
           ,[DeviceChannel]
           ,[CurrentTemperatureLocationId])
     VALUES
           ('Ambientweather-F007TH'
           ,96
           ,'1'
           ,1)

INSERT INTO [dbo].[TemperatureDevices]
           ([DeviceModel]
           ,[DeviceId]
           ,[DeviceChannel]
           ,[CurrentTemperatureLocationId])
     VALUES
           ('Ambientweather-F007TH'
           ,9
           ,'2'
           ,2)

INSERT INTO [dbo].[TemperatureDevices]
           ([DeviceModel]
           ,[DeviceId]
           ,[DeviceChannel]
           ,[CurrentTemperatureLocationId])
     VALUES
           ('Acurite-986'
           ,1369
           ,'1R'
           ,3)

INSERT INTO [dbo].[TemperatureDevices]
           ([DeviceModel]
           ,[DeviceId]
           ,[DeviceChannel]
           ,[CurrentTemperatureLocationId])
     VALUES
           ('Acurite-986'
           ,1254
           ,'2F'
           ,4)

UPDATE [HomeSensorsDev].[dbo].[TemperatureReadings] SET
	TemperatureDeviceId = td.Id,
	TemperatureLocationId = td.CurrentTemperatureLocationId
FROM TemperatureReadings tr
INNER JOIN TemperatureDevices td ON tr.DeviceModel = td.DeviceModel AND tr.DeviceId = td.DeviceId AND tr.DeviceChannel = td.DeviceChannel

SELECT * FROM [HomeSensorsDev].[dbo].[TemperatureReadings]

SELECT tl.Name, tr.TemperatureCelsius FROM [HomeSensorsDev].[dbo].[TemperatureReadings] tr
LEFT join TemperatureLocations tl on tl.Id = tr.TemperatureLocationId
