UPDATE d SET
d.Name = l.Name,
d.MqttTopic = 'rtl_433/' + d.DeviceModel + '/' + d.DeviceId + '/' + d.DeviceChannel
FROM
    TemperatureDevices d
    INNER JOIN TemperatureLocations l ON d.TemperatureLocationId = l.Id

SELECT * FROM TemperatureDevices
