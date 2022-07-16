using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeSensors.Web.Models;
using HomeSensors.Data;
using Microsoft.EntityFrameworkCore;

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
        const int intervalMinutes = 15;
        var startTime = DateTimeOffset.Now.AddHours(-72);

        var data = _data.TemperatureReadings
            .Include(x => x.TemperatureLocation)
            .Where(x => x.TemperatureLocationId != null)
            .Where(x => x.Time > startTime)
            .OrderByDescending(x => x.Time)
            .AsEnumerable()
            .GroupBy(x => x.TemperatureLocation?.Name ?? "unknown")
            .OrderBy(x => x.Key);

        var dbSeries = data
            .Select(locationGroup =>
            {
                var groups = locationGroup
                    .GroupBy(y =>
                    {
                        // Round down to 30 minute intervals and zero milliseconds and seconds to make period-starting groups.
                        var time = y.Time;
                        time = time.AddMinutes(-(time.Minute % intervalMinutes));
                        time = time.AddMilliseconds(-time.Millisecond - (1000 * time.Second));
                        return time;
                    })
                    .Select(timeGroup =>
                    {
                        var intervalAverage = timeGroup.Average(s => s.TemperatureCelsius);

                        return new GraphPointViewModel
                        {
                            Time = timeGroup.Key,
                            TemperatureCelsius = intervalAverage
                        };
                    })
                    .ToList();

                return new GraphSeriesViewModel(locationGroup.Key, groups);
            })
            .ToList();

        var currentTemps = data
            .Select(l =>
            {
                var reading = l.First();

                return new GraphCurrentReading(l.Key, reading.TemperatureCelsius, reading.Time);
            });

        var graph = new GraphViewModel
        {
            Series = dbSeries,
            Current = currentTemps,
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

    public class GraphViewModel
    {
        public IEnumerable<GraphSeriesViewModel> Series { get; init; } = Array.Empty<GraphSeriesViewModel>();
        public IEnumerable<GraphCurrentReading> Current { get; init; } = Array.Empty<GraphCurrentReading>();
    }

    public class GraphSeriesViewModel
    {
        public GraphSeriesViewModel(string location, IEnumerable<GraphPointViewModel> points)
        {
            Location = location;
            Points = points;
        }

        public string Location { get; }
        public IEnumerable<GraphPointViewModel> Points { get; }
    }

    public class GraphPointViewModel
    {
        public double? TemperatureCelsius { get; init; }
        public DateTimeOffset Time { get; init; }
    }

    public class GraphCurrentReading
    {
        public string Location { get; }
        public double? TemperatureCelsius { get; }
        public DateTimeOffset Time { get; }

        public GraphCurrentReading(string location, double? temperatureCelsius, DateTimeOffset time)
        {
            Location = location;
            TemperatureCelsius = temperatureCelsius;
            Time = time;
        }
    }
}
