using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Hvac.Entities;

public class HvacActionConfiguration : IEntityTypeConfiguration<HvacAction>
{
    public void Configure(EntityTypeBuilder<HvacAction> entity)
    {
        entity.ToTable("HvacAction");

        // Index for HvacActionsWorker duplicate prevention
        entity.HasIndex(a => new { a.EntityId, a.LastChanged, a.LastUpdated })
            .IncludeProperties(a => new { a.State })
            .IsUnique();

        // Index for GetHvacActionsAsync time range queries
        entity.HasIndex(a => a.LastUpdated)
            .IncludeProperties(a => new { a.EntityId, a.State, a.LastChanged });
    }
}
