namespace ChatAgent.App.Configurations;

public class AIAgentConfiguration
{
    public static string SectionName = "AIAgent";

    public string? ApiKey { get; set; }

    public string? ApiUrl { get; set; }

    public string? ModelId { get; set; }
}
