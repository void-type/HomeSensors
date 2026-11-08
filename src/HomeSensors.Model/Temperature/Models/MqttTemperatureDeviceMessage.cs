using System.Text.Json.Serialization;

namespace HomeSensors.Model.Temperature.Models;

/// <summary>
/// A temperature sensor message published by rtl_433.
/// </summary>
/// <remarks>
/// Documentation for sensors: https://triq.org/rtl_433/DATA_FORMAT.html
/// <para>Sample JSON:</para>
/// <code>
/// {
///   "time":"2022-07-08 06:36:39",
///   "model":"Acurite-986",
///   "id":1369,
///   "channel":"1R",
///   "battery_ok":1,
///   "temperature_C":-15,
///   "status":0,
///   "mic":"CRC"
/// }
/// </code>
/// <code>
/// {
///   "time": "2022-07-08 21:10:23Z",
///   "model": "Ambientweather-F007TH",
///   "id": 96,
///   "channel": 1,
///   "battery_ok": 1,
///   "temperature_C": 30.83333,
///   "humidity": 40,
///   "mic": "CRC"
/// }
/// </code>
/// </remarks>
public class MqttTemperatureDeviceMessage
{
    public DateTimeOffset Time { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Id { get; set; } = string.Empty;
    public string? Channel { get; set; } = string.Empty;
    [JsonPropertyName("battery_ok")]
    public double? BatteryOk { get; set; }
    public int? Status { get; set; }
    public string? Mic { get; set; } = string.Empty;
    [JsonPropertyName("temperature_C")]
    public double? TemperatureC { get; set; }
    public double? Humidity { get; set; }
}
