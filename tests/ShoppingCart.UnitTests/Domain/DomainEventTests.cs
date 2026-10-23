using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.UnitTests.Domain;

public class DomainEventTests
{
    [Fact]
    public void EventId_and_OccurredOn_are_assigned_once_and_stable_across_reads()
    {
        IDomainEvent domainEvent = new CartClearedEvent(CartId.Of(Guid.NewGuid()), 1);

        var firstId = domainEvent.EventId;
        var firstOccurredOn = domainEvent.OccurredOn;

        Assert.NotEqual(Guid.Empty, firstId);
        Assert.Equal(firstId, domainEvent.EventId);
        Assert.Equal(firstOccurredOn, domainEvent.OccurredOn);
        Assert.Equal(DateTimeKind.Utc, firstOccurredOn.Kind);
    }

    [Fact]
    public void Each_event_gets_its_own_id()
    {
        var cartId = CartId.Of(Guid.NewGuid());

        Assert.NotEqual(new CartClearedEvent(cartId, 1).EventId, new CartClearedEvent(cartId, 1).EventId);
    }

    [Fact]
    public void EventType_is_the_class_name()
    {
        IDomainEvent domainEvent = new CartClearedEvent(CartId.Of(Guid.NewGuid()), 1);

        Assert.Equal(nameof(CartClearedEvent), domainEvent.EventType);
    }
}
