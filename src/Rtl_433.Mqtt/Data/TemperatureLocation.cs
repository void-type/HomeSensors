namespace Rtl_433.Mqtt.Data
{
    public class TemperatureLocation
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public virtual List<TemperatureReading> TemperatureReadings { get; set; } = new();
    }
}
