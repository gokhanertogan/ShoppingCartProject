using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Persistence.Common.Serialization;

namespace ShoppingCart.UnitTests.Persistence;

public class EventSerializerTests
{
    private readonly EventSerializer _serializer = new();

    public static TheoryData<IDomainEvent> Events()
    {
        var cartId = CartId.Of(Guid.NewGuid());
        var itemId = CartItemId.Of(Guid.NewGuid());
        var variantId = VariantId.Of(Guid.NewGuid());
        var merchantId = MerchantId.Of(Guid.NewGuid());

        return
        [
            new CartCreatedEvent(cartId, CartGuid.Of(Guid.NewGuid())),
            new CartItemAddedEvent(cartId, itemId, variantId, merchantId, 10.25m, 2),
            new CartItemUpdatedEvent(cartId, itemId, 10.25m, 5),
            new CartItemRemovedEvent(cartId, itemId, variantId, merchantId),
            new CartClearedEvent(cartId, 3),
        ];
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void Round_trips_every_domain_event_with_its_payload_and_identity(IDomainEvent domainEvent)
    {
        var json = _serializer.Serialize(domainEvent);

        var restored = _serializer.Deserialize(EventSerializer.GetEventType(domainEvent), json);

        Assert.IsType(domainEvent.GetType(), restored);
        // Records compare by value, including EventId and OccurredOn.
        Assert.Equal(domainEvent, restored);
    }

    [Fact]
    public void Writes_strongly_typed_ids_as_plain_guids()
    {
        var cartId = CartId.Of(Guid.NewGuid());

        var json = _serializer.Serialize(new CartClearedEvent(cartId, 1));

        Assert.Contains($"\"cartId\":\"{cartId.Value}\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_unknown_event_types()
    {
        Assert.Throws<InvalidOperationException>(() => _serializer.Deserialize("NoSuchEvent", "{}"));
    }
}
