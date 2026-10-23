using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Common.Events;

namespace ShoppingCart.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));

        // Register every IDomainEventHandler<T> implementation of this assembly.
        var handlerRegistrations =
            from type in assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
            select (contract, type);

        foreach (var (contract, implementation) in handlerRegistrations)
        {
            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
