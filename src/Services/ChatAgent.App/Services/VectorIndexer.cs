using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChatAgent.App.Services;

/// <inheritdoc cref="IVectorIndexer"/>
public class VectorIndexer : IVectorIndexer
{
    private readonly ChatAgentDbContext _dbContext;

    public VectorIndexer(ChatAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertAsync(
        VectorSourceType sourceType,
        string sourceId,
        string content,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var metadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata);

        var existing = await _dbContext.VectorDocuments
            .FirstOrDefaultAsync(
                x => x.SourceType == sourceType && x.SourceId == sourceId,
                cancellationToken);

        if (existing is null)
        {
            _dbContext.VectorDocuments.Add(new VectorDocument
            {
                Id = Guid.NewGuid(),
                SourceType = sourceType,
                SourceId = sourceId,
                Content = content,
                Metadata = metadataJson,
                Embedding = null,
                EmbeddedAt = null,
                UpdatedAt = DateTime.UtcNow,
            });

            return;
        }

        // CDC is at-least-once and replays are common. Re-embedding identical text would
        // burn tokens and briefly blank the vector for no benefit, so only invalidate when
        // the embedded text actually moved.
        var contentChanged = !string.Equals(existing.Content, content, StringComparison.Ordinal);

        existing.Content = content;
        existing.Metadata = metadataJson;
        existing.UpdatedAt = DateTime.UtcNow;

        if (contentChanged)
        {
            existing.Embedding = null;
            existing.EmbeddedAt = null;
        }
    }

    public async Task RemoveAsync(
        VectorSourceType sourceType,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.VectorDocuments
            .FirstOrDefaultAsync(
                x => x.SourceType == sourceType && x.SourceId == sourceId,
                cancellationToken);

        if (existing is not null)
        {
            _dbContext.VectorDocuments.Remove(existing);
        }
    }
}
