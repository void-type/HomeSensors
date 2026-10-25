using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Temperature.Entities;

public class TemperatureLocationConfiguration : IEntityTypeConfiguration<TemperatureLocation>
{
    public void Configure(EntityTypeBuilder<TemperatureLocation> entity)
    {
        entity.ToTable(nameof(TemperatureLocation));

        entity.HasIndex(l => l.Name)
            .IsUnique();

        entity.HasOne(r => r.Category)
            .WithMany(i => i.TemperatureLocations).HasForeignKey("CategoryId");
    }
}
