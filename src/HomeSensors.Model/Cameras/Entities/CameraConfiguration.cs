using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Cameras.Entities;

public class CameraConfiguration : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> entity)
    {
        entity.ToTable(nameof(Camera));

        entity.HasIndex(c => c.Name)
            .IsUnique();
    }
}
