using ChatAgent.App.Data.Enums;
using ChatAgent.App.Guards;

namespace ChatAgent.App.Data.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public ChatMessageRole Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// What the response guard decided about this message.
    /// </summary>
    /// <remarks>
    /// Null for customer messages, and for refusals that never reached the reviewer. Without it an
    /// approved answer and a guard-rejected fallback look identical in the transcript, which is
    /// precisely the distinction anyone auditing the guard needs.
    /// </remarks>
    public ReviewVerdictKind? ReviewVerdict { get; set; }
}
