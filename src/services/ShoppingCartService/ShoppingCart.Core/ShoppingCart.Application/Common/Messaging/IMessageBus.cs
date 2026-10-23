namespace ShoppingCart.Application.Common.Messaging;

public interface IMessageBus
{
    /// <summary>Publishes a message and waits until the broker has confirmed it.</summary>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts consuming messages of type <typeparamref name="T"/> from a durable queue. The message is
    /// acknowledged after <paramref name="handler"/> completes and rejected if it throws.
    /// </summary>
    Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default);
}
