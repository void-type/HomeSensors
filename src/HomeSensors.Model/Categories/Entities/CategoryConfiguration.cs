using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Categories.Entities;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable(nameof(Category));

        entity.HasIndex(c => c.Order);

        entity.HasIndex(si => si.Name)
            .IsUnique();
    }
}
