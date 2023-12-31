using HomeSensors.Model.Helpers;
using Xunit;

namespace HomeSensors.Test.Model;

public class DateTimeExtensionsTests
{
    [Theory]
    [InlineData(5, 20, 15, 20, 19)]
    [InlineData(5, 20, 20, 20, 20)]
    [InlineData(5, 20, 20, 20, 21)]

    [InlineData(10, 20, 10, 20, 19)]
    [InlineData(10, 20, 20, 20, 20)]
    [InlineData(10, 20, 20, 20, 21)]

    [InlineData(15, 20, 0, 20, 14)]
    [InlineData(15, 20, 15, 20, 15)]
    [InlineData(15, 20, 15, 20, 19)]

    [InlineData(20, 20, 0, 20, 19)]
    [InlineData(20, 20, 20, 20, 20)]
    [InlineData(20, 20, 20, 20, 21)]

    [InlineData(30, 20, 0, 20, 29)]
    [InlineData(30, 20, 30, 20, 30)]
    [InlineData(30, 20, 30, 20, 31)]

    [InlineData(1, 20, 31, 20, 31)]

    [InlineData(60, 20, 0, 20, 31)]
    [InlineData(60, 20, 0, 20, 0)]
    public void Minutes_round_down_to_nearest_10(int interval, int expectedHour, int expectedMinute, int actualHour, int actualMinute)
    {
        // Ensure the offset is preserved, ticks and microseconds are cleared out.
        var actual = new DateTimeOffset(2023, 10, 31, actualHour, actualMinute, 23, 17, 13, TimeSpan.FromHours(-6))
            .AddTicks(7)
            .RoundDownMinutes(interval);

        Assert.Equal(
            new DateTimeOffset(2023, 10, 31, expectedHour, expectedMinute, 00, 00, TimeSpan.FromHours(-6)),
            actual);
    }

    [Fact]
    public void Throws_when_interval_above_60()
    {
        Assert.Throws<ArgumentException>(() => new DateTimeOffset(2023, 10, 31, 20, 30, 00, 00, new TimeSpan()).RoundDownMinutes(62));
    }

    [Fact]
    public void Throws_when_interval_below_1()
    {
        Assert.Throws<ArgumentException>(() => new DateTimeOffset(2023, 10, 31, 20, 30, 00, 00, new TimeSpan()).RoundDownMinutes(0));
    }
}
