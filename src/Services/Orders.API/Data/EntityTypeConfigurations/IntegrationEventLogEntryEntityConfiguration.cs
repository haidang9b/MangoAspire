using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.API.Data.EntityTypeConfigurations;

public class IntegrationEventLogEntryEntityConfiguration : IEntityTypeConfiguration<IntegrationEventLogEntry>
{
    public void Configure(EntityTypeBuilder<IntegrationEventLogEntry> builder)
    {
        builder.HasKey(e => e.EventId);
    }
}
