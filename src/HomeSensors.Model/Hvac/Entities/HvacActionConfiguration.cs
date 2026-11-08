using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Hvac.Entities;

public class HvacActionConfiguration : IEntityTypeConfiguration<HvacAction>
{
    public void Configure(EntityTypeBuilder<HvacAction> builder)
    {
        builder.ToTable("HvacAction");

        // Index for HvacActionsWorker duplicate prevention
        builder.HasIndex(a => new { a.EntityId, a.LastChanged, a.LastUpdated })
            .IncludeProperties(a => new { a.State })
            .IsUnique();

        // Index for GetHvacActionsAsync time range queries
        builder.HasIndex(a => a.LastUpdated)
            .IncludeProperties(a => new { a.EntityId, a.State, a.LastChanged });
    }
}
