using ShoppingCart.Application.Cart.Events;
using ShoppingCart.Application.Common.Messaging;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.UnitTests.Application;

public class CartIntegrationEventPublisherTests
{
    private sealed class RecordingMessageBus : IMessageBus
    {
        public List<object> Published { get; } = [];

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            Published.Add(message!);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task Publishes_an_integration_event_that_keeps_the_domain_event_id()
    {
        var bus = new RecordingMessageBus();
        var publisher = new CartIntegrationEventPublisher(bus);
        var domainEvent = new CartItemAddedEvent(
            CartId.Of(Guid.NewGuid()), CartItemId.Of(Guid.NewGuid()), VariantId.Of(Guid.NewGuid()), MerchantId.Of(Guid.NewGuid()), 9.99m, 3);

        await publisher.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        var published = Assert.IsType<CartItemAddedIntegrationEvent>(Assert.Single(bus.Published));
        Assert.Equal(domainEvent.EventId, published.EventId);
        Assert.Equal(domainEvent.OccurredOn, published.OccurredOn);
        Assert.Equal(domainEvent.CartId.Value, published.CartId);
        Assert.Equal(domainEvent.VariantId.Value, published.VariantId);
        Assert.Equal(3, published.Quantity);
    }
}
