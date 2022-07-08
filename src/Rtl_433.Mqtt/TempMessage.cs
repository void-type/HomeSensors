namespace Rtl_433.Mqtt
{
    public class TempMessage
    {
        // TODO: convert to DateTimeOffset before saving.
        public string TimeString { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Id { get; set; }
        public double BatteryOk { get; set; }
        public double TemperatureCelsius { get; set; }
        public int Status { get; set; }
        public string Mic { get; set; } = string.Empty;
    }

    // Sample JSON
    // {
    //   "time":"2022-07-08 06:36:39",
    //   "model":"Acurite-986",
    //   "id":1369,"channel":"1R",
    //   "battery_ok":1,
    //   "temperature_C":-15,
    //   "status":0,
    //   "mic":"CRC"
    // }
}
