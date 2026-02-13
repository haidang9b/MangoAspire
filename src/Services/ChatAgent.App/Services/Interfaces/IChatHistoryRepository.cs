using ChatAgent.App.Data.Entities;
using ChatAgent.App.Dtos;
using Mango.Core.Pagination;

namespace ChatAgent.App.Services.Interfaces;

public interface IChatHistoryRepository
{
    Task<PaginatedItems<ChatMessageDto>> GetRecentMessagesAsync(string userId, int pageSize = 10, int pageIndex = 1);
    Task AddMessageAsync(ChatMessage message);
    Task ClearHistoryAsync(string userId);
}
