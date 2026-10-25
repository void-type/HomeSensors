using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.WaterLeak.Entities;

public class WaterLeakDeviceConfiguration : IEntityTypeConfiguration<WaterLeakDevice>
{
    public void Configure(EntityTypeBuilder<WaterLeakDevice> entity)
    {
        entity.ToTable(nameof(WaterLeakDevice));

        entity.HasIndex(d => d.MqttTopic)
            .IsUnique();
    }
}
