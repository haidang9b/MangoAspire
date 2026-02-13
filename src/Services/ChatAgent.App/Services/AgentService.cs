using ChatAgent.App.Data.Enums;
using ChatAgent.App.Dtos;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatAgent.App.Services;

public class AgentService : IAgentService
{
    private readonly Kernel _kernel;
    private readonly ICartPlugin _cartPlugin;
    private readonly IProductsPlugin _productsPlugin;
    private readonly ICouponsPlugin _couponsPlugin;
    private readonly ICheckoutPlugin _checkoutPlugin;
    private readonly IWebSearchPlugin _webSearchPlugin;
    private readonly IChatHistoryMemoryStorage _chatHistory;

    public AgentService(
        Kernel kernel,
        ICartPlugin cartPlugin,
        IChatHistoryMemoryStorage chatHistory,
        IProductsPlugin productPlugin,
        ICouponsPlugin couponsPlugin,
        ICheckoutPlugin checkoutPlugin,
        IWebSearchPlugin webSearchPlugin
    )
    {
        _kernel = kernel;
        _cartPlugin = cartPlugin;
        _chatHistory = chatHistory;
        _productsPlugin = productPlugin;
        _couponsPlugin = couponsPlugin;
        _checkoutPlugin = checkoutPlugin;
        _webSearchPlugin = webSearchPlugin;
    }

    public async IAsyncEnumerable<string> ChatStreamingAsync(string userId, PromptRequestDto promptRequest)
    {
        // 1. Clone kernel (important for scoped plugins)
        var scopedKernel = _kernel.Clone();

        // 2. Import plugins from DI
        scopedKernel.ImportPluginFromObject(_productsPlugin);
        scopedKernel.ImportPluginFromObject(_cartPlugin);
        scopedKernel.ImportPluginFromObject(_couponsPlugin);
        scopedKernel.ImportPluginFromObject(_checkoutPlugin);
        scopedKernel.ImportPluginFromObject(_webSearchPlugin);

        var chatHistory = await _chatHistory.GetChatHistoryAsync(userId);

        chatHistory.AddUserMessage(promptRequest.Content);

        // Save user message to database
        await _chatHistory.SaveMessageAsync(userId, ChatMessageRole.User, promptRequest.Content);

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

        // Save assistant response to database
        await _chatHistory.SaveMessageAsync(userId, ChatMessageRole.Assistant, fullResponse);
    }

}
