using HomeSensors.Model.TemperatureRepositories.Models;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Temperatures;

/// <summary>
/// SignalR hub that clients can connect to for real-time temperature data.
/// </summary>
public class TemperatureHub : Hub
{
    private readonly CachedTemperatureRepository _temperatureRepository;

    public TemperatureHub(CachedTemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    public Task<List<GraphCurrentReading>> GetCurrentReadings()
    {
        return _temperatureRepository.GetCurrentReadings();
    }
}
