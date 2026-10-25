using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Temperature.Entities;

public class TemperatureReadingConfiguration : IEntityTypeConfiguration<TemperatureReading>
{
    public void Configure(EntityTypeBuilder<TemperatureReading> entity)
    {
        entity.ToTable(nameof(TemperatureReading));

        // Index for GetCurrentAsync query
        entity.HasIndex(r => new { r.Time })
            .HasFilter("[IsSummary] = 0")
            .IncludeProperties(r => new { r.TemperatureLocationId, r.TemperatureCelsius, r.Humidity });

        // Index for GetTimeSeriesAsync query
        entity.HasIndex(r => new { r.TemperatureLocationId, r.Time })
            .IncludeProperties(r => new { r.TemperatureCelsius, r.Humidity });

        // Index for SummarizeTemperatureReadingsWorker.ExecuteAsync oldReadings query
        entity.HasIndex(r => new { r.IsSummary, r.TemperatureDeviceId, r.Time })
            .IncludeProperties(r => new { r.Id, r.TemperatureCelsius, r.Humidity, r.TemperatureLocationId });

        // Index for GetAllAsync in TemperatureDeviceRepository
        entity.HasIndex(r => new { r.TemperatureDeviceId, r.Time })
            .HasDatabaseName("IX_TemperatureReadings_DeviceLatest")
            .IncludeProperties(r => new { r.TemperatureCelsius, r.Humidity, r.DeviceBatteryLevel, r.TemperatureLocationId });

        entity.HasOne(x => x.TemperatureLocation)
            .WithMany(x => x.TemperatureReadings)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
