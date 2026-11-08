using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Temperature.Entities;

public class TemperatureLocationConfiguration : IEntityTypeConfiguration<TemperatureLocation>
{
    public void Configure(EntityTypeBuilder<TemperatureLocation> builder)
    {
        builder.ToTable(nameof(TemperatureLocation));

        builder.HasIndex(l => l.Name)
            .IsUnique();

        builder.HasOne(r => r.Category)
            .WithMany(i => i.TemperatureLocations).HasForeignKey("CategoryId");
    }
}
