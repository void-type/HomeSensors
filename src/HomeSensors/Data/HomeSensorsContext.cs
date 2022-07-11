using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HomeSensors.Data
{
    public class HomeSensorsContext : DbContext
    {
        public HomeSensorsContext(DbContextOptions<HomeSensorsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TemperatureDevice> TemperatureDevices { get; set; }
        public virtual DbSet<TemperatureLocation> TemperatureLocations { get; set; }
        public virtual DbSet<TemperatureReading> TemperatureReadings { get; set; }
    }
}
