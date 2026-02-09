namespace EventBus.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EventNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
