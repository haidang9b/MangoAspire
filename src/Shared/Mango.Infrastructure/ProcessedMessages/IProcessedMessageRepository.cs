namespace Mango.Infrastructure.ProcessedMessages;

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(Guid id, string typeName);

    Task CreateAsync(Guid id, string typeName);
}
