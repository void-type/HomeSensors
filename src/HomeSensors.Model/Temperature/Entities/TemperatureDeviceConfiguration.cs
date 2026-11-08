using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Temperature.Entities;

public class TemperatureDeviceConfiguration : IEntityTypeConfiguration<TemperatureDevice>
{
    public void Configure(EntityTypeBuilder<TemperatureDevice> builder)
    {
        builder.ToTable(nameof(TemperatureDevice));

        builder.HasIndex(d => d.MqttTopic)
            .IsUnique();
    }
}
