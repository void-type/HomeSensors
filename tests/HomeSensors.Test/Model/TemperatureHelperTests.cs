using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Helpers;
using Xunit;

namespace HomeSensors.Test.Model;

public class TemperatureHelperTests
{

    [Fact]
    public void Average_0_minute_readings()
    {
        var readings = new List<TemperatureReading>
        {
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 0, 0, new TimeSpan()),
                TemperatureCelsius = 20
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 1, 0, new TimeSpan()),
                TemperatureCelsius = 30
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 2, 0, new TimeSpan()),
                TemperatureCelsius = 20
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 4, 0, new TimeSpan()),
                TemperatureCelsius = 30
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 5, 0, new TimeSpan()),
                TemperatureCelsius = 200
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 6, 0, new TimeSpan()),
                TemperatureCelsius = 220
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 7, 0, new TimeSpan()),
                TemperatureCelsius = 200
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 8, 0, new TimeSpan()),
                TemperatureCelsius = 220
            }
        };

        var averages = readings.GetIntervalAverages(0);

        Assert.Equal(8, averages.Count);
    }
    [Fact]
    public void Average_5_minute_readings()
    {
        var readings = new List<TemperatureReading>
        {
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 0, 0, new TimeSpan()),
                TemperatureCelsius = 20
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 1, 0, new TimeSpan()),
                TemperatureCelsius = 30
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 2, 0, new TimeSpan()),
                TemperatureCelsius = 20
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 4, 0, new TimeSpan()),
                TemperatureCelsius = 30
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 5, 0, new TimeSpan()),
                TemperatureCelsius = 200
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 6, 0, new TimeSpan()),
                TemperatureCelsius = 220
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 7, 0, new TimeSpan()),
                TemperatureCelsius = 200
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 8, 0, new TimeSpan()),
                TemperatureCelsius = 220
            }
        };

        var averages = readings.GetIntervalAverages(5);

        Assert.Equal(2, averages.Count);

        Assert.Equal(25, averages[0].TemperatureCelsius);
        Assert.Equal(new DateTimeOffset(2023, 10, 30, 20, 0, 0, new TimeSpan()), averages[0].Time);

        Assert.Equal(210, averages[1].TemperatureCelsius);
        Assert.Equal(new DateTimeOffset(2023, 10, 30, 20, 5, 0, new TimeSpan()), averages[1].Time);
    }

    [Fact]
    public void Average_10_minute_readings()
    {
        var readings = new List<TemperatureReading>
        {
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 0, 0, new TimeSpan()),
                TemperatureCelsius = 20
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 1, 0, new TimeSpan()),
                TemperatureCelsius = 30
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 2, 0, new TimeSpan()),
                TemperatureCelsius = 20
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 4, 0, new TimeSpan()),
                TemperatureCelsius = 30
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 5, 0, new TimeSpan()),
                TemperatureCelsius = 200
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 6, 0, new TimeSpan()),
                TemperatureCelsius = 220
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 7, 0, new TimeSpan()),
                TemperatureCelsius = 200
            },
            new() {
                Time = new DateTimeOffset(2023, 10, 30, 20, 8, 0, new TimeSpan()),
                TemperatureCelsius = 220
            }
        };

        var averages = readings.GetIntervalAverages(10);

        Assert.Single(averages);
        Assert.Equal(117.5, averages[0].TemperatureCelsius);
    }

    [Fact]
    public void Dual_temp_string_and_format()
    {
        Assert.Equal("-40°F / -40°C", TemperatureHelpers.GetDualTempString(-40));
        Assert.Equal("212°F / 100°C", TemperatureHelpers.GetDualTempString(100));
        Assert.Equal("32°F / 0°C", TemperatureHelpers.GetDualTempString(0));
    }
}
