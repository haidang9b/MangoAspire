namespace ChatAgent.App.Services.Interfaces;

/// <summary>
/// Ingests the markdown store documents into the retrieval index on startup, skipping
/// files whose contents have not changed since the last run.
/// </summary>
public interface IKnowledgeBaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
