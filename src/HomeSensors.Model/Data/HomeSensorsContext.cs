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

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<TemperatureDevice> TemperatureDevices { get; set; }
    public virtual DbSet<TemperatureLocation> TemperatureLocations { get; set; }
    public virtual DbSet<TemperatureReading> TemperatureReadings { get; set; }
    public virtual DbSet<ThermostatAction> ThermostatActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable(nameof(Category));

            entity.HasIndex(c => c.Order);

            entity.HasIndex(si => si.Name)
                .IsUnique();
        });

        modelBuilder.Entity<TemperatureReading>(entity =>
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
                .IncludeProperties(r => new { r.TemperatureCelsius, r.Humidity, r.TemperatureLocationId });

            entity.HasOne(x => x.TemperatureLocation)
                .WithMany(x => x.TemperatureReadings)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TemperatureLocation>(entity =>
        {
            entity.ToTable(nameof(TemperatureLocation));

            entity.HasIndex(l => l.Name)
                .IsUnique();

            entity.HasOne(r => r.Category)
                .WithMany(i => i.TemperatureLocations).HasForeignKey("CategoryId");
        });

        modelBuilder.Entity<TemperatureDevice>(entity =>
        {
            entity.ToTable(nameof(TemperatureDevice));

            entity.HasIndex(d => d.MqttTopic)
                .IsUnique();
        });

        modelBuilder.Entity<ThermostatAction>(entity =>
        {
            entity.ToTable(nameof(ThermostatAction));

            entity.HasIndex(a => new { a.EntityId, a.LastChanged, a.LastUpdated })
                .IncludeProperties(a => new { a.State })
                .IsUnique();
        });
    }
}
