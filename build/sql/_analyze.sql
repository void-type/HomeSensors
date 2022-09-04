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

-- Average times
SELECT
  tr.[Time]
, td.DeviceModel
FROM [dbo].[TemperatureReadings] tr
  INNER JOIN [dbo].[TemperatureDevices] td ON td.[Id] = tr.[TemperatureDeviceId]
  LEFT JOIN [dbo].[TemperatureLocations] tl ON tl.[Id] = tr.[TemperatureLocationId]
where td.Id = 7
  AND Time >= DateAdd(SECOND, -14400, (select Convert(DateTime2, max(time))
  from TemperatureReadings))
ORDER BY [Time] ASC

-- $first, $times = Get-Content .\times.csv | % { [DateTimeOffset]::Parse($_) }; $times | % { $temp = $first; $first = $_; ($_ - $temp).TotalSeconds } | measure -Average | select -expand Average
-- Ambientweather-F007TH 56.95
-- Ambientweather-F007TH 58.77
-- Ambientweather-F007TH 61.45
-- Ambientweather-F007TH 62.99
-- Acurite-986 144.29
-- Acurite-986 122.68
-- Acurite-Tower 16.90

-- Monthly averages
SELECT
  l.[Name] AS LocationName
      , ROUND(MIN([TemperatureCelsius]), 2) as MinTemp
      , ROUND(AVG([TemperatureCelsius]), 2) as AvgTemp
      , ROUND(MAX([TemperatureCelsius]), 2) as MaxTemp
      , ROUND(MIN([Humidity]), 2) as MinHumidity
      , ROUND(AVG([Humidity]), 2) as AvgHumidity
      , ROUND(MAX([Humidity]), 2) as MaxHumidity
FROM [HomeSensors].[dbo].[TemperatureReadings] r
  INNER JOIN TemperatureLocations l ON l.Id = r.TemperatureLocationId
-- LEFT JOIN TemperatureDevices d ON d.Id = r.TemperatureDeviceId
WHERE r.[Time] BETWEEN '2022-08-01 00:00:00' AND '2022-08-31 00:00:00'
GROUP BY l.Name
