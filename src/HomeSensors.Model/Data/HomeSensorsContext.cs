using HomeSensors.Model.Cameras.Entities;
using HomeSensors.Model.Categories.Entities;
using HomeSensors.Model.Hvac.Entities;
using HomeSensors.Model.Infrastructure.Emailing.Entities;
using HomeSensors.Model.Temperature.Entities;
using HomeSensors.Model.WaterLeak.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace HomeSensors.Model.Data;

public class HomeSensorsContext : DbContext
{
    [SuppressMessage("Usage", "MA0056:Do not call overridable members in constructor", Justification = "ChangeTracker is the standard DbContext configuration point and is not overridden.")]
    public HomeSensorsContext(DbContextOptions<HomeSensorsContext> options)
        : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public virtual DbSet<Camera> Cameras { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<TemperatureDevice> TemperatureDevices { get; set; }
    public virtual DbSet<TemperatureLocation> TemperatureLocations { get; set; }
    public virtual DbSet<TemperatureReading> TemperatureReadings { get; set; }
    public virtual DbSet<HvacAction> HvacActions { get; set; }
    public virtual DbSet<WaterLeakDevice> WaterLeakDevices { get; set; }
    public virtual DbSet<EmailRecipient> EmailRecipients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HomeSensorsContext).Assembly);
    }
}
