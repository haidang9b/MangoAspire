using Microsoft.EntityFrameworkCore;

namespace Mango.Infrastructure.ProcessedMessages;

public class ProcessedMessageRepository<TDbContext>(TDbContext dbContext) : IProcessedMessageRepository
    where TDbContext : DbContext
{
    public async Task<bool> ExistsAsync(Guid id, string typeName)
    {
        return await dbContext.Set<ProcessedMessage>()
            .AnyAsync(m => m.Id == id && m.Name == typeName);
    }

    public async Task CreateAsync(Guid id, string typeName)
    {
        var processedMessage = new ProcessedMessage
        {
            Id = id,
            Name = typeName,
            ProcessedDate = DateTime.UtcNow
        };

        await dbContext.Set<ProcessedMessage>().AddAsync(processedMessage);
        await dbContext.SaveChangesAsync();
    }
}
