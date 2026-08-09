using ChatAgent.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatAgent.App.Data.EntityTypeConfigurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    /// <summary>
    /// Column width for a stored message. Must match
    /// <see cref="GuardOptions.MaxStoredMessageChars"/>, which is what truncates on the way in.
    /// </summary>
    public const int MaxContentLength = 4000;

    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Role)
            .IsRequired();

        // Bounded rather than unlimited text: customer messages are capped by the input guard, and
        // assistant answers are model-generated and therefore unbounded at the source.
        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(MaxContentLength);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ReviewVerdict);

        // Indexes for performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
