namespace HomeSensors.Service.Models;

public class TemperatureMessage
{
    public DateTimeOffset Time { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Id { get; set; } = string.Empty;
    public string? Channel { get; set; } = string.Empty;
    public double? Battery_Ok { get; set; }
    public int? Status { get; set; }
    public string? Mic { get; set; } = string.Empty;
    public double? Temperature_C { get; set; }
    public double? Humidity { get; set; }
}

#pragma warning disable S125
// Documentation for sensors
// https://triq.org/rtl_433/DATA_FORMAT.html
// Sample JSON
// {
//   "time":"2022-07-08 06:36:39",
//   "model":"Acurite-986",
//   "id":1369,
//   "channel":"1R",
//   "battery_ok":1,
//   "temperature_C":-15,
//   "status":0,
//   "mic":"CRC"
// }

// {
//     "time": "2022-07-08 21:10:23Z",
//     "model": "Ambientweather-F007TH",
//     "id": 96,
//     "channel": 1,
//     "battery_ok": 1,
//     "temperature_C": 30.83333,
//     "humidity": 40,
//     "mic": "CRC"
// }
