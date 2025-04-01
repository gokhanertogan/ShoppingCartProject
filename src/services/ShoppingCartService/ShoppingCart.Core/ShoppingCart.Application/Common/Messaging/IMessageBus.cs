namespace ShoppingCart.Application.Common.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(T message);
    Task SubscribeAsync<T>(Func<T, Task> handler);
}