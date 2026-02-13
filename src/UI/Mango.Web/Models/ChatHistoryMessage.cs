namespace Mango.Web.Models;

public class ChatHistoryMessage
{
    public Guid Id { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChatHistoryResponse
{
    public List<ChatHistoryMessage> Messages { get; set; } = [];
    public int PageSize { get; set; }
    public int Skip { get; set; }
    public bool HasMore { get; set; }
}

public enum ChatMessageRole
{
    System = 0,
    User = 1,
    Assistant = 2
}
