using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Hubs;

/// <summary>
/// SignalR hub that clients can connect to for real-time temperature data.
/// </summary>
public class TemperaturesHub : Hub
{
    public const string newMessageMessageName = "newDiscoveryMessage";
    public const string UpdateCurrentReadingsMessageName = "updateCurrentReadings";

    private readonly TemperatureCachedRepository _temperatureRepository;

    public TemperaturesHub(TemperatureCachedRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    public Task<List<Reading>> GetCurrentReadings()
    {
        return _temperatureRepository.GetCurrentReadings();
    }
}
