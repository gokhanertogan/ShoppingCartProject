using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Common.Events;
using ShoppingCart.Domain.Common;
using ShoppingCart.Persistence.Common.Events.Interfaces;

namespace ShoppingCart.Persistence.Common.Events;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo HandleMethod)> HandlerCache = new();

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        foreach (var domainEvent in domainEvents)
        {
            var (handlerType, handleMethod) = HandlerCache.GetOrAdd(domainEvent.GetType(), static eventType =>
            {
                var type = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
                return (type, type.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!);
            });

            foreach (var handler in scope.ServiceProvider.GetServices(handlerType))
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
