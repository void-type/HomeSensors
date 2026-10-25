using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Temperature.Entities;

public class TemperatureDeviceConfiguration : IEntityTypeConfiguration<TemperatureDevice>
{
    public void Configure(EntityTypeBuilder<TemperatureDevice> entity)
    {
        entity.ToTable(nameof(TemperatureDevice));

        entity.HasIndex(d => d.MqttTopic)
            .IsUnique();
    }
}
