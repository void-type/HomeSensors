using HomeSensors.Model.Repositories.Models;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Alerts;

public class DeviceAlert : ValueObject
{
    public DeviceAlert(DeviceAlertType type, Device device, Location? location, DateTimeOffset resendAfter)
    {
        Type = type;
        Device = device;
        Location = location;
        ResendAfter = resendAfter;
    }

    public DeviceAlertType Type { get; }
    public Device Device { get; }
    public Location? Location { get; }
    public DateTimeOffset ResendAfter { get; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return Device.Id;
    }
}
