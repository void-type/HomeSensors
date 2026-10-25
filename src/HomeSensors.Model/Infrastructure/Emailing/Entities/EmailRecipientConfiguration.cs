using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSensors.Model.Infrastructure.Emailing.Entities;

public class EmailRecipientConfiguration : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> entity)
    {
        entity.ToTable(nameof(EmailRecipient));

        entity.HasIndex(e => e.Email)
            .IsUnique();
    }
}
