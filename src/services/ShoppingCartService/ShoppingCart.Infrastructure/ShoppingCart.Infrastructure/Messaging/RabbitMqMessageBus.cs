using ShoppingCart.Application.Common.Messaging;

namespace ShoppingCart.Infrastructure.Messaging;

public class RabbitMqMessageBus : IMessageBus
{
    public Task PublishAsync<T>(T message)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeAsync<T>(Func<T, Task> handler)
    {
        throw new NotImplementedException();
    }
}
