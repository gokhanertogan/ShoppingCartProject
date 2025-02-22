using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Common.Events;
using ShoppingCart.Domain.Common;
using ShoppingCart.Persistence.Common.Events.Interfaces;

namespace ShoppingCart.Persistence.Common.Events;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        using var scope = _serviceProvider.CreateScope();
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                await (Task)handlerType.GetMethod("HandleAsync")!.Invoke(handler, [domainEvent])!;
            }
        }
    }
}