using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EventBus.Abstractions;

public class EventBusSubscriptionInfo
{
    public Dictionary<SubscriptionInfo, Type> SubscriptionTypes { get; } = [];

    public Dictionary<string, Type> QueueTypes { get; } = [];

    public Dictionary<string, Type> ConsumerTypes { get; } = [];

    public Dictionary<string, Type> TopicTypes { get; } = [];

    public JsonSerializerOptions JsonSerializerOptions { get; } = new(DefaultSerializerOptions);

    internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? CreateDefaultTypeResolver() : JsonTypeInfoResolver.Combine()
    };

#pragma warning disable IL2026
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
    private static IJsonTypeInfoResolver CreateDefaultTypeResolver()
        => new DefaultJsonTypeInfoResolver();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026
}


public record SubscriptionInfo(string TopicName, string SubscriptionName)
{
    public override string ToString() => $"topic:{TopicName}.subscription:{SubscriptionName}";
}


public record ConsumerInfo(string QueueName)
{
    public override string ToString() => $"queue:{QueueName}";
}
