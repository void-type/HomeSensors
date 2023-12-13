namespace HomeSensors.Model.Repositories.Models;

public class DeviceUpdateRequest
{
    public long Id { get; init; }
    public long? CurrentLocationId { get; init; }
    public bool IsRetired { get; init; }
}
