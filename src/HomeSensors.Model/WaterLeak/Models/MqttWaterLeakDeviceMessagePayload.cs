using System.Text.Json.Serialization;

namespace HomeSensors.Model.WaterLeak.Models;

/// <summary>
/// A water leak sensor payload published by zigbee2mqtt.
/// </summary>
/// <remarks>
/// Documentation for sensors: https://www.zigbee2mqtt.io/devices/3RWS18BZ.html#third%2520reality-3rws18bz
/// <para>Sample JSON:</para>
/// <code>
/// {
///   "battery": 100,
///   "battery_low": false,
///   "linkquality": 144,
///   "tamper": false,
///   "water_leak": false
/// }
/// </code>
/// </remarks>
public class MqttWaterLeakDeviceMessagePayload
{
    public double? Battery { get; set; }
    [JsonPropertyName("battery_low")]
    public bool? BatteryLow { get; set; }
    public int? LinkQuality { get; set; }
    public bool? Tamper { get; set; }
    [JsonPropertyName("water_leak")]
    public bool? WaterLeak { get; set; }
}
