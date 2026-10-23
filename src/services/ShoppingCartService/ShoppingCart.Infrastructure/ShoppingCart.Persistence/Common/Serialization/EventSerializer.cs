using System.Collections.Frozen;
using System.Text.Json;
using ShoppingCart.Application.Common.Serialization;
using ShoppingCart.Domain.Common;

namespace ShoppingCart.Persistence.Common.Serialization;

/// <summary>
/// Serializes domain events by their concrete type and resolves them back from a stable type name
/// (the class name, e.g. <c>CartItemAddedEvent</c>), so stored events do not depend on assembly
/// names or versions.
/// </summary>
public sealed class EventSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private readonly FrozenDictionary<string, Type> _eventTypes = typeof(IDomainEvent).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(type))
        .ToFrozenDictionary(type => type.Name, StringComparer.Ordinal);

    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        return options;
    }

    public static string GetEventType(IDomainEvent domainEvent) => domainEvent.GetType().Name;

    public string Serialize(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType();
        if (!_eventTypes.ContainsKey(type.Name))
        {
            throw new InvalidOperationException($"Domain event type '{type.FullName}' is not registered.");
        }

        // Serialize by the runtime type; serializing through the interface would drop the payload.
        return JsonSerializer.Serialize(domainEvent, type, Options);
    }

    public IDomainEvent Deserialize(string eventType, string data)
    {
        if (!_eventTypes.TryGetValue(eventType, out var type))
        {
            throw new InvalidOperationException($"Unknown domain event type '{eventType}'.");
        }

        return (IDomainEvent)(JsonSerializer.Deserialize(data, type, Options)
            ?? throw new InvalidOperationException($"Event payload of type '{eventType}' is empty."));
    }
}
