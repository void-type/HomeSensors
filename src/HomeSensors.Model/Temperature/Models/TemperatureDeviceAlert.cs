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

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(TemperatureDeviceAlert? left, TemperatureDeviceAlert? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(TemperatureDeviceAlert? left, TemperatureDeviceAlert? right) => !(left == right);

    public static bool operator <(TemperatureDeviceAlert? left, TemperatureDeviceAlert? right) => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(TemperatureDeviceAlert? left, TemperatureDeviceAlert? right) => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(TemperatureDeviceAlert? left, TemperatureDeviceAlert? right) => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(TemperatureDeviceAlert? left, TemperatureDeviceAlert? right) => left is null ? right is null : left.CompareTo(right) >= 0;
}
