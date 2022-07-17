using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;
using Microsoft.AspNetCore.SignalR;

namespace HomeSensors.Web.Temperatures;

/// <summary>
/// This registers a SignalR hub that clients can connect to for real-time data.
/// </summary>
public class TemperatureHub : Hub
{
    private readonly TemperatureRepository _temperatureRepository;

    public TemperatureHub(TemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }
    public Task<List<GraphCurrentReading>> GetCurrentReadings()
    {
        return _temperatureRepository.GetCurrentReadings();
    }
}
