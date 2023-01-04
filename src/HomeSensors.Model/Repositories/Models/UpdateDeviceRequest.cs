namespace HomeSensors.Model.Repositories.Models;

public class UpdateDeviceRequest
{
    public long Id { get; init; }
    public long? CurrentLocationId { get; init; }
    public bool IsRetired { get; init; }
}
