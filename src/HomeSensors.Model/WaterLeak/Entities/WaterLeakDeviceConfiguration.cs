using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.WaterLeak.Entities;

public class WaterLeakDeviceConfiguration : IEntityTypeConfiguration<WaterLeakDevice>
{
    public void Configure(EntityTypeBuilder<WaterLeakDevice> builder)
    {
        builder.ToTable(nameof(WaterLeakDevice));

        builder.HasIndex(d => d.MqttTopic)
            .IsUnique();
    }
}
