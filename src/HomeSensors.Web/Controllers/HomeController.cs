using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeSensors.Web.Models;
using HomeSensors.Data.Repositories;
using HomeSensors.Data.Repositories.Models;

namespace HomeSensors.Web.Controllers;

public class HomeController : Controller
{
    private readonly TemperatureRepository _temperatureRepository;

    public HomeController(TemperatureRepository temperatureRepository)
    {
        _temperatureRepository = temperatureRepository;
    }

    public async Task<IActionResult> Index()
    {
        var startTime = DateTimeOffset.Now.AddHours(-48);
        var endTime = DateTimeOffset.Now;
        var intervalMinutes = 15;

        var readings = await _temperatureRepository.GetCurrentReadings();
        var series = await _temperatureRepository.GetTemperatureTimeSeries(startTime, endTime, intervalMinutes);

        var graph = new GraphViewModel
        {
            Current = readings,
            Series = series,
        };

        // TODO: Fahrenheit selector
        // TODO: SignalR / API with date selectors and granularity selector
        // Make live readings prettier
        return View("Index", graph);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
