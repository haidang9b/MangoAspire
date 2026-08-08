namespace ChatAgent.App.Services.Interfaces;

/// <param name="Ordinal">Position within the document, used for stable ordering.</param>
/// <param name="Breadcrumb">Heading path, e.g. <c>Store Info &gt; Refunds &gt; Late deliveries</c>.</param>
/// <param name="Content">Text to embed and index, already prefixed with the breadcrumb.</param>
public record MarkdownChunk(int Ordinal, string Breadcrumb, string Content);

/// <summary>
/// Splits a markdown document into retrievable chunks that fit the embedding model's
/// input window without losing the context needed to retrieve them.
/// </summary>
public interface IMarkdownChunker
{
    IReadOnlyList<MarkdownChunk> Chunk(string markdown, KnowledgeBaseOptions options);
}
