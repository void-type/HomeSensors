namespace HomeSensors.Model.Services.WaterLeak;

public class MqttWaterLeakDeviceMessagePayload
{
    public double? Battery { get; set; }
    public bool? Battery_Low { get; set; }
    public int? LinkQuality { get; set; }
    public bool? Tamper { get; set; }
    public bool? Water_Leak { get; set; }
}

#pragma warning disable S125
// Documentation for sensors
// https://www.zigbee2mqtt.io/devices/3RWS18BZ.html#third%2520reality-3rws18bz
// Sample JSON
// {
//   "battery": 100,
//   "battery_low": false,
//   "linkquality": 144,
//   "tamper": false,
//   "water_leak": false
// }
