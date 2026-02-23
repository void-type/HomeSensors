using HomeSensors.Model.WaterLeak.Models;
using System.Collections.Concurrent;

namespace HomeSensors.Model.WaterLeak.Services;

public class WaterLeakAlertState
{
    public ConcurrentDictionary<WaterLeakDeviceAlert, DateTimeOffset> LatchedMessageAlerts { get; } = new();

    public ConcurrentDictionary<string, DateTimeOffset> LastCheckIns { get; } = new();

    public ConcurrentDictionary<string, DateTimeOffset> LatchedInactiveAlerts { get; } = new();
}
