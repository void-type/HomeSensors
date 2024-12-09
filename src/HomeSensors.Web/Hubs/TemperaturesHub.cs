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

    // Omit async suffix for hub methods
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

    public async Task<List<TemperatureReadingResponse>> GetCurrentReadings()
    {
        return await _readingRepository.GetCurrentCachedAsync();
    }
}
