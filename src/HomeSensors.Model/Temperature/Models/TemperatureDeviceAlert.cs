using VoidCore.Model.Functional;

namespace HomeSensors.Model.Temperature.Models;

public class TemperatureDeviceAlert(TemperatureDeviceAlertType type, TemperatureDeviceResponse device, TemperatureLocationResponse? location, DateTimeOffset resendAfter) : ValueObject
{
    public TemperatureDeviceAlertType Type { get; } = type;
    public TemperatureDeviceResponse Device { get; } = device;
    public TemperatureLocationResponse? Location { get; } = location;
    public DateTimeOffset ResendAfter { get; } = resendAfter;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return Device.Id;
    }
}
