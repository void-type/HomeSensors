using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HomeSensors.Web.Health;

/// <summary>
/// Reports unhealthy when no temperature reading has been recorded recently. This catches a stopped MQTT worker or
/// service, which have no endpoint of their own to check.
/// </summary>
public class TemperatureReadingsHealthCheck(
    IDbContextFactory<HomeSensorsContext> contextFactory,
    TimeProvider timeProvider,
    HealthChecksSettings settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var latest = await dbContext.TemperatureReadings
            .TagWith($"Query called from {nameof(TemperatureReadingsHealthCheck)}.")
            .Where(x => !x.IsSummary)
            .MaxAsync(x => (DateTimeOffset?)x.Time, cancellationToken);

        return Evaluate(latest, timeProvider.GetUtcNow(), TimeSpan.FromMinutes(settings.MaxReadingAgeMinutes));
    }

    public static HealthCheckResult Evaluate(DateTimeOffset? latestReading, DateTimeOffset now, TimeSpan maxAge)
    {
        if (latestReading is null)
        {
            return HealthCheckResult.Unhealthy("No temperature readings have been recorded.");
        }

        var age = now - latestReading.Value;

        return age > maxAge
            ? HealthCheckResult.Unhealthy($"The latest temperature reading is {(int)age.TotalMinutes} minutes old. The limit is {(int)maxAge.TotalMinutes} minutes.")
            : HealthCheckResult.Healthy($"The latest temperature reading is {(int)age.TotalMinutes} minutes old.");
    }
}
