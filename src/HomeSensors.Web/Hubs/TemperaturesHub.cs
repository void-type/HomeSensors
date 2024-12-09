using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Hubs;

/// <summary>
/// SignalR hub that clients can connect to for real-time temperature data.
/// </summary>
public class TemperaturesHub : Hub
{
    public const string NewDiscoveryMessageMessageName = "newDiscoveryMessage";
    public const string UpdateCurrentReadingsMessageName = "updateCurrentReadings";

    private readonly TemperatureReadingRepository _readingRepository;

    public TemperaturesHub(TemperatureReadingRepository readingRepository)
    {
        _readingRepository = readingRepository;
    }

    public async Task<List<TemperatureReadingResponse>> GetCurrentReadingsAsync()
    {
        return await _readingRepository.GetCurrentCachedAsync();
    }
}
