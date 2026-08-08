using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Tests;

/// <summary>
/// The production context for use with the in-memory provider.
/// </summary>
/// <remarks>
/// pgvector's <c>Vector</c> and Postgres' <c>tsvector</c> have no in-memory equivalent, so
/// those two columns are ignored here. Everything the tests actually assert on — the
/// replicated rows, the index entries, and the embedded/not-embedded state — is unaffected.
/// Retrieval itself depends on those columns and is verified against a real database.
/// </remarks>
public class TestChatAgentDbContext : ChatAgentDbContext
{
    public TestChatAgentDbContext(DbContextOptions<TestChatAgentDbContext> options) : base(options)
    {
    }

    public static TestChatAgentDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestChatAgentDbContext>()
            .UseInMemoryDatabase($"chatagent-{Guid.NewGuid()}")
            .Options;

        return new TestChatAgentDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VectorDocument>(builder =>
        {
            builder.Ignore(x => x.Embedding);
            builder.Ignore(x => x.SearchVector);
        });
    }
}
