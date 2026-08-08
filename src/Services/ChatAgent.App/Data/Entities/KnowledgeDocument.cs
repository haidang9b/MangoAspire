namespace ChatAgent.App.Data.Entities;

/// <summary>
/// Ingestion ledger for knowledge-base markdown files: which ones have been read, and at
/// what content. <see cref="Services.KnowledgeBaseSeeder"/> compares
/// <see cref="ContentHash"/> on every startup so unchanged files are skipped entirely —
/// no re-chunking and no embedding spend.
/// </summary>
public class KnowledgeDocument
{
    public Guid Id { get; set; }

    /// <summary>Path relative to the knowledge-base root, so it is stable across machines.</summary>
    public required string SourcePath { get; set; }

    public required string Title { get; set; }

    /// <summary>Hex SHA-256 of the file contents at the time it was ingested.</summary>
    public required string ContentHash { get; set; }

    public int ChunkCount { get; set; }

    public DateTime IngestedAt { get; set; }
}
