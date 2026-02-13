using ChatAgent.App.Models;
using ChatAgent.App.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatAgent.App.Services;

public class AgentService : IAgentService
{
    private readonly Kernel _kernel;

    private readonly CartPlugin _cartPlugin;

    private readonly ChatHistoryMemoryStorage _chatHistory;


    public AgentService(Kernel kernel, CartPlugin cartPlugin, ChatHistoryMemoryStorage chatHistory)
    {
        _kernel = kernel;
        _cartPlugin = cartPlugin;
        _chatHistory = chatHistory;
    }

    public async IAsyncEnumerable<string> ChatStreamingAsync(string userId, PromptRequest promptRequest)
    {
        // 1. Clone kernel (important for scoped plugins)
        var scopedKernel = _kernel.Clone();

        // 2. Import plugins from DI
        //scopedKernel.ImportPluginFromObject(_productPlugin);
        scopedKernel.ImportPluginFromObject(_cartPlugin);
        //scopedKernel.ImportPluginFromObject(_voucherPlugin);

        var chatHistory = _chatHistory.GetChatHistory(userId);

        chatHistory.AddUserMessage(promptRequest.Content);

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        // 3. Use streaming WITH kernel
        var chatService = scopedKernel.GetRequiredService<IChatCompletionService>();
        string fullResponse = string.Empty;
        await foreach (var response in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            scopedKernel
            ))
        {
            if (!string.IsNullOrEmpty(response.Content))
            {
                fullResponse += response.Content;
                yield return response.Content;
            }
        }

        chatHistory.AddAssistantMessage(fullResponse);
    }

}
