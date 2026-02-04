using HomeSensors.Model.Temperature.Models;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Web.Services.MqttDiscovery;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Hubs;

/// <summary>
/// SignalR hub that clients can connect to for real-time temperature data.
/// </summary>
public class TemperaturesHub : Hub
{
    public const string NewDiscoveryMessageMessageName = "newDiscoveryMessage";
    public const string UpdateCurrentReadingsMessageName = "updateCurrentReadings";
    public const string UpdateCategoriesMessageName = "updateCategories";
    public const string UpdateDiscoveryStatusMessageName = "updateDiscoveryStatus";

    private readonly TemperatureReadingRepository _readingRepository;
    private readonly MqttDiscoveryService _discoveryService;

    public TemperaturesHub(TemperatureReadingRepository readingRepository, MqttDiscoveryService discoveryService)
    {
        _readingRepository = readingRepository;
        _discoveryService = discoveryService;
    }

    // Omit async suffix for hub methods
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

    public async Task<List<TemperatureReadingResponse>> GetCurrentReadings()
    {
        return await _readingRepository.GetCurrentCachedAsync();
    }

    public MqttDiscoveryClientStatus GetDiscoveryStatus()
    {
        return _discoveryService.GetClientStatus();
    }

    public async Task SetupDiscovery(MqttDiscoverySetupRequest request)
    {
        var result = await _discoveryService.SetupClientAsync(request);

        if (result.IsFailed)
        {
            throw new HubException(string.Join("; ", result.Failures.Select(f => f.Message)));
        }
    }

    public async Task TeardownDiscovery()
    {
        await _discoveryService.TeardownClientAsync();
    }
}
