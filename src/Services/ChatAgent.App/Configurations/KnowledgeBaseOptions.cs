namespace ChatAgent.App.Configurations;

/// <summary>
/// Controls how markdown store documents are discovered, split and ingested.
/// </summary>
public class KnowledgeBaseOptions
{
    /// <summary>Folder scanned for <c>*.md</c>, relative to the content root.</summary>
    public string Path { get; set; } = "KnowledgeBase";

    /// <summary>
    /// Upper bound on a chunk, in characters. Roughly four characters per token, so the
    /// default sits far inside text-embedding-3-small's 8k-token input window while
    /// staying small enough that a hit is specific rather than "the whole policy page".
    /// </summary>
    public int MaxChunkChars { get; set; } = 1200;

    /// <summary>
    /// Chunks below this are merged into their neighbour where possible, so a document of
    /// one-line bullets does not explode into hundreds of near-useless fragments.
    /// </summary>
    public int MinChunkChars { get; set; } = 200;

    /// <summary>
    /// Trailing characters of the previous chunk repeated at the start of the next, so a
    /// sentence spanning a boundary is still retrievable. Applied within a section only.
    /// </summary>
    public int ChunkOverlapChars { get; set; } = 150;

    /// <summary>Files larger than this are skipped with a warning rather than ingested.</summary>
    public long MaxDocumentBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Chunks flushed to the database per round-trip during ingestion.</summary>
    public int IngestBatchSize { get; set; } = 200;
}
