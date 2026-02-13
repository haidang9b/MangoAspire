namespace ChatAgent.App.Plugins.Interfaces;

public interface IWebSearchPlugin
{
    Task<string> SearchWebAsync(string query);
}
