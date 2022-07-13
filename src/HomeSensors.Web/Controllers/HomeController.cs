using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeSensors.Web.Models;
using HomeSensors.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Generic;

namespace HomeSensors.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HomeSensorsContext _data;

    public HomeController(ILogger<HomeController> logger, HomeSensorsContext data)
    {
        _logger = logger;
        _data = data;
    }

    public IActionResult Index()
    {
        // var data = _data.TemperatureReadings
        //     .Include(x => x.TemperatureLocation)
        //     .OrderByDescending(x => x.Time)
        //     .Where(x => x.Time > DateTimeOffset.Now.AddDays(-1))
        //     .Where(x => x.TemperatureLocationId != null)
        //     .GroupBy(x => x.TemperatureLocation!.Name)
        //     .OrderBy(x => x.Key)
        //     .Select(x =>
        //         new
        //         {
        //             label = x.Key,
        //             values = x.Select(x => x.TemperatureCelsius)
        //         }
        //     );

        var startTime = DateTimeOffset.Now.AddDays(-1);

        var readings = _data.TemperatureReadings
            .Include(x => x.TemperatureLocation)
            .OrderByDescending(x => x.Time)
            .Where(x => x.Time > startTime)
            .Where(x => x.TemperatureLocationId != null);

        var data = readings
            .GroupBy(x => x.TemperatureLocation!.Name)
            .OrderBy(x => x.Key)
            .AsEnumerable()
            .Select(x =>
            {
                var groups = x.GroupBy(y =>
                {
                    // Round to nearest 30 minutes and zero milliseconds and seconds.
                    var time = y.Time;
                    time = time.AddMinutes(-(time.Minute % 30));
                    time = time.AddMilliseconds(-time.Millisecond - (1000 * time.Second));
                    return time;
                })
                .Select(g => new TemperatureTimeSeries { Time = g.Key, Temperature = g.Average(s => s.TemperatureCelsius) })
                .ToList();

                return new TemperatureData
                {
                    Label = x.Key,
                    TimeSeries = groups
                };
            })
            .ToList();

        var allTimes = data.SelectMany(x => x.TimeSeries.Select(x => x.Time)).Distinct();

        return View("Index", data);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public class TemperatureData
    {
        public string Label { get; set; } = string.Empty;
        public IEnumerable<TemperatureTimeSeries> TimeSeries { get; set; } = Array.Empty<TemperatureTimeSeries>();
        public IEnumerable<DateTimeOffset> AllTimes => TimeSeries.Select(x => x.Time).Distinct();
    }

    public class TemperatureTimeSeries
    {
        public DateTimeOffset Time { get; set; }
        public double? Temperature { get; set; }
    }
}
