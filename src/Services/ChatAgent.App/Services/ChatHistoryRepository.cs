using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Dtos;
using Mango.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Services;

public class ChatHistoryRepository : IChatHistoryRepository
{
    private readonly ChatAgentDbContext _dbContext;

    public ChatHistoryRepository(ChatAgentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedItems<ChatMessageDto>> GetRecentMessagesAsync(string userId, int pageSize = 10, int pageIndex = 1)
    {
        var totalItems = await _dbContext.ChatMessages
            .Where(m => m.UserId == userId)
            .CountAsync();

        var skip = (Math.Max(0, pageIndex - 1)) * pageSize;

        var messages = await _dbContext.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Paginate(pageIndex, pageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync();

        // Reverse to chronological order for display
        messages.Reverse();

        return new PaginatedItems<ChatMessageDto>(pageIndex, pageSize, totalItems, messages);
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearHistoryAsync(string userId)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m => m.UserId == userId)
            .ToListAsync();

        _dbContext.ChatMessages.RemoveRange(messages);
        await _dbContext.SaveChangesAsync();
    }
}
