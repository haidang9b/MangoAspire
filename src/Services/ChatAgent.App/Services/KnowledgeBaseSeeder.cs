using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChatAgent.App.Services;

/// <inheritdoc cref="IKnowledgeBaseSeeder"/>
/// <remarks>
/// The <c>knowledge_documents</c> table is the ledger of what has already been read. Each
/// file is hashed and compared against its ledger row, so an unchanged document costs one
/// hash per startup — no re-chunking and no embedding spend — while an edited one is
/// re-ingested from scratch.
/// </remarks>
public class KnowledgeBaseSeeder : IKnowledgeBaseSeeder
{
    private readonly ChatAgentDbContext _dbContext;
    private readonly IMarkdownChunker _chunker;
    private readonly KnowledgeBaseOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<KnowledgeBaseSeeder> _logger;

    public KnowledgeBaseSeeder(
        ChatAgentDbContext dbContext,
        IMarkdownChunker chunker,
        IOptions<AIAgentConfiguration> options,
        IHostEnvironment environment,
        ILogger<KnowledgeBaseSeeder> logger)
    {
        _dbContext = dbContext;
        _chunker = chunker;
        _options = options.Value.KnowledgeBase;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var root = Path.IsPathRooted(_options.Path)
            ? _options.Path
            : Path.Combine(_environment.ContentRootPath, _options.Path);

        if (!Directory.Exists(root))
        {
            _logger.LogWarning("Knowledge base folder {Path} does not exist; nothing to ingest.", root);
            return;
        }

        var files = Directory.GetFiles(root, "*.md", SearchOption.AllDirectories);
        _logger.LogInformation("Scanning {Count} knowledge base document(s) in {Path}.", files.Length, root);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await IngestFileAsync(root, file, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One malformed document must not stop the service from starting; the
                // others still ingest and this one is retried on the next startup.
                _logger.LogError(ex, "Failed to ingest knowledge base document {File}.", file);
            }
        }

        await RemoveDeletedDocumentsAsync(root, files, cancellationToken);
    }

    private async Task IngestFileAsync(string root, string file, CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');

        var fileInfo = new FileInfo(file);
        if (fileInfo.Length > _options.MaxDocumentBytes)
        {
            _logger.LogWarning(
                "Skipping {File}: {Size} bytes exceeds MaxDocumentBytes ({Limit}).",
                relativePath, fileInfo.Length, _options.MaxDocumentBytes);
            return;
        }

        var content = await File.ReadAllTextAsync(file, cancellationToken);
        var hash = ComputeHash(content);

        var document = await _dbContext.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.SourcePath == relativePath, cancellationToken);

        if (document is not null && document.ContentHash == hash)
        {
            _logger.LogDebug("Knowledge base document {File} is unchanged; skipping.", relativePath);
            return;
        }

        var chunks = _chunker.Chunk(content, _options);
        if (chunks.Count == 0)
        {
            _logger.LogWarning("Knowledge base document {File} produced no chunks.", relativePath);
            return;
        }

        var documentId = document?.Id ?? Guid.NewGuid();

        // One transaction per document, so a crash mid-ingest can never leave the ledger
        // claiming a file was read when only some of its chunks landed.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var sourceId = documentId.ToString();
            await _dbContext.VectorDocuments
                .Where(v => v.SourceType == VectorSourceType.KnowledgeChunk && v.SourceId == sourceId)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var batch in chunks.Chunk(Math.Max(1, _options.IngestBatchSize)))
            {
                _dbContext.VectorDocuments.AddRange(batch.Select(chunk => new VectorDocument
                {
                    Id = Guid.NewGuid(),
                    SourceType = VectorSourceType.KnowledgeChunk,
                    SourceId = sourceId,
                    Content = chunk.Content,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        SourcePath = relativePath,
                        chunk.Breadcrumb,
                        chunk.Ordinal,
                    }),
                    // Left unembedded on purpose: EmbeddingBackfillService picks these up
                    // asynchronously, so startup never waits on Azure OpenAI.
                    Embedding = null,
                    EmbeddedAt = null,
                    UpdatedAt = DateTime.UtcNow,
                }));

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            if (document is null)
            {
                _dbContext.KnowledgeDocuments.Add(new KnowledgeDocument
                {
                    Id = documentId,
                    SourcePath = relativePath,
                    Title = ExtractTitle(content, relativePath),
                    ContentHash = hash,
                    ChunkCount = chunks.Count,
                    IngestedAt = DateTime.UtcNow,
                });
            }
            else
            {
                document.Title = ExtractTitle(content, relativePath);
                document.ContentHash = hash;
                document.ChunkCount = chunks.Count;
                document.IngestedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogInformation(
            "Ingested knowledge base document {File} into {Count} chunk(s).", relativePath, chunks.Count);
    }

    /// <summary>
    /// Drops ledger rows and chunks for documents that have been removed from the folder,
    /// so the agent stops citing a policy that no longer exists.
    /// </summary>
    private async Task RemoveDeletedDocumentsAsync(
        string root,
        string[] files,
        CancellationToken cancellationToken)
    {
        var present = files
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stale = await _dbContext.KnowledgeDocuments
            .Where(d => !present.Contains(d.SourcePath))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        var staleIds = stale.Select(d => d.Id.ToString()).ToList();

        await _dbContext.VectorDocuments
            .Where(v => v.SourceType == VectorSourceType.KnowledgeChunk && staleIds.Contains(v.SourceId))
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.KnowledgeDocuments.RemoveRange(stale);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Removed {Count} knowledge base document(s) that no longer exist on disk.", stale.Count);
    }

    private static string ComputeHash(string content)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    /// <summary>First level-1 heading, falling back to the file name.</summary>
    private static string ExtractTitle(string content, string relativePath)
    {
        foreach (var line in content.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                return line[2..].Trim();
            }
        }

        return Path.GetFileNameWithoutExtension(relativePath);
    }
}
