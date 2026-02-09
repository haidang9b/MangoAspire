using Mango.Core.Domain;

namespace Mango.Infrastructure.ProcessedMessages;

public class ProcessedMessage : EntityBase<Guid>
{
    public required string Name { get; set; }

    public DateTime ProcessedDate { get; set; }
}
