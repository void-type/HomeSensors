using HomeSensors.Web.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace HomeSensors.Test;

public class TemperatureReadingsHealthCheckTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _maxAge = TimeSpan.FromMinutes(30);

    [Fact]
    public void Evaluate_is_unhealthy_without_readings()
    {
        var result = TemperatureReadingsHealthCheck.Evaluate(null, _now, _maxAge);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public void Evaluate_is_healthy_when_reading_is_recent()
    {
        var result = TemperatureReadingsHealthCheck.Evaluate(_now.AddMinutes(-5), _now, _maxAge);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("5 minutes", result.Description);
    }

    [Fact]
    public void Evaluate_is_healthy_at_the_limit()
    {
        var result = TemperatureReadingsHealthCheck.Evaluate(_now.AddMinutes(-30), _now, _maxAge);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void Evaluate_is_unhealthy_when_reading_is_stale()
    {
        var result = TemperatureReadingsHealthCheck.Evaluate(_now.AddMinutes(-31), _now, _maxAge);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("31 minutes", result.Description);
    }
}
