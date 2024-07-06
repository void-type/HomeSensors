using HomeSensors.Model.Data.Models;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HomeSensors.Model.Data;

public class HomeSensorsContext : DbContext
{
    public HomeSensorsContext(DbContextOptions<HomeSensorsContext> options)
        : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public virtual DbSet<TemperatureDevice> TemperatureDevices { get; set; }
    public virtual DbSet<TemperatureLocation> TemperatureLocations { get; set; }
    public virtual DbSet<TemperatureReading> TemperatureReadings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemperatureReading>(entity =>
        {
            entity.ToTable(nameof(TemperatureReading));

            entity.HasIndex(r => r.Time);
            entity.HasIndex(r => r.IsSummary);
            entity.HasIndex(r => new { r.Time, r.TemperatureLocationId });
            entity.HasIndex(r => new { r.Time, r.TemperatureDeviceId, r.IsSummary });
        });

        modelBuilder.Entity<TemperatureLocation>(entity =>
        {
            entity.ToTable(nameof(TemperatureLocation));

            entity.HasIndex(r => r.Name)
                .IsUnique();
        });

        modelBuilder.Entity<TemperatureDevice>(entity =>
        {
            entity.ToTable(nameof(TemperatureDevice));

            entity.HasIndex(r => r.MqttTopic)
                .IsUnique();
        });
    }
}
