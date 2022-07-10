using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Rtl_433.Mqtt.Data
{
    public class HomeSensorsContext : DbContext
    {
        public HomeSensorsContext(DbContextOptions<HomeSensorsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TemperatureReading> TemperatureReadings { get; set; }
        public virtual DbSet<TemperatureDevice> TemperatureDevices { get; set; }
    }
}
