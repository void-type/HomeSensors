using HomeSensors.Model.Repositories.Models;
using HomeSensors.Web.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Hubs;

/// <summary>
/// SignalR hub that clients can connect to for real-time temperature data.
/// </summary>
public class TemperatureHub : Hub
{
    private readonly TemperatureCachedRepository _temperatureRepository;

    public TemperatureHub(TemperatureCachedRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    public Task<List<Reading>> GetCurrentReadings()
    {
        return _temperatureRepository.GetCurrentReadings();
    }
}
