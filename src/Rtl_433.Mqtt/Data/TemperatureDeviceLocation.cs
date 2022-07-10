namespace Rtl_433.Mqtt.Data;

public class TemperatureDeviceLocation
{
    public long Id { get; set; }
    public DateTimeOffset StartTime { get; set; }

    public long TemperatureDeviceId { get; set; }
    public virtual TemperatureDevice TemperatureDevice { get; set; } = new();

    public long TemperatureLocationId { get; set; }
    public virtual TemperatureLocation TemperatureLocation { get; set; } = new();
}
