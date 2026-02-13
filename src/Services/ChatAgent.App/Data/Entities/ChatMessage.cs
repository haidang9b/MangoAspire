using ChatAgent.App.Data.Enums;

namespace ChatAgent.App.Data.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public ChatMessageRole Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
