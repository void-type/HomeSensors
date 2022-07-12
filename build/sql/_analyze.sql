-- Latest readings
SELECT TOP (1000)
  tr.[Id]
, tr.[Time]
, td.[DeviceModel]
, td.[DeviceId]
, td.[DeviceChannel]
, tl.[Name]
, tr.[TemperatureCelsius]
, tr.[Humidity]
FROM [dbo].[TemperatureReadings] tr
  INNER JOIN [dbo].[TemperatureDevices] td ON td.[Id] = tr.[TemperatureDeviceId]
  LEFT JOIN [dbo].[TemperatureLocations] tl ON tl.[Id] = tr.[TemperatureLocationId]
ORDER BY [Time] DESC


-- Latest readings for each device
SELECT
  td.[DeviceModel]
, td.[DeviceId]
, td.[DeviceChannel]
, tl.[Name]
, tr.[Time]
, tr.[TemperatureCelsius]
, tr.[Humidity]
, tr.[DeviceBatteryLevel]
, tr.[DeviceStatus]
FROM [dbo].[TemperatureReadings] tr
  INNER JOIN [dbo].[TemperatureDevices] td ON td.[Id] = tr.[TemperatureDeviceId]
  LEFT JOIN [dbo].[TemperatureLocations] tl ON tl.[Id] = tr.[TemperatureLocationId]
  INNER JOIN
  (
    SELECT
    trInner.[TemperatureDeviceId]
    , Max(trInner.[Time]) Time
  FROM [dbo].[TemperatureReadings] trInner
  GROUP BY trInner.[TemperatureDeviceId]
  ) AS trMax
  ON tr.[TemperatureDeviceId] = trMax.[TemperatureDeviceId] AND tr.[Time] = trMax.[Time]
ORDER BY tl.[Name]

-- All readings for a specific device
SELECT TOP (1000)
  tr.[Time]
, tr.[TemperatureCelsius]
, tr.[Humidity]
FROM [dbo].[TemperatureReadings] tr
  INNER JOIN [dbo].[TemperatureDevices] td ON td.[Id] = tr.[TemperatureDeviceId]
  LEFT JOIN [dbo].[TemperatureLocations] tl ON tl.[Id] = tr.[TemperatureLocationId]
where td.Id = 3
ORDER BY [Time] DESC

-- Readings and devices that have no location
SELECT *
FROM [dbo].[TemperatureReadings]
WHERE [TemperatureLocationId] IS NULL

SELECT *
FROM [dbo].[TemperatureDevices]
WHERE [CurrentTemperatureLocationId] IS NULL
