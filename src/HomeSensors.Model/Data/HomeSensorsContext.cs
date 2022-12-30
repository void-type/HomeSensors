using HomeSensors.Model.Data.Models;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HomeSensors.Model.Data;

public class HomeSensorsContext : DbContext
{
    public HomeSensorsContext(DbContextOptions<HomeSensorsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TemperatureDevice> TemperatureDevices { get; set; }
    public virtual DbSet<TemperatureLocation> TemperatureLocations { get; set; }
    public virtual DbSet<TemperatureReading> TemperatureReadings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemperatureReading>()
            .HasIndex(r => r.Time);

        modelBuilder.Entity<TemperatureReading>()
            .HasIndex(r => r.IsSummary);

        modelBuilder.Entity<TemperatureLocation>()
            .HasIndex(r => r.Name)
            .IsUnique();
    }
}
