using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Persistence.Cart.Repositories;
using ShoppingCart.Persistence.Common.Data;
using ShoppingCart.Persistence.Common.Events;
using ShoppingCart.Persistence.Common.Events.Interfaces;
using ShoppingCart.Persistence.Common.Outbox;
using ShoppingCart.Persistence.Common.Serialization;

namespace ShoppingCart.Persistence;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ShoppingCartDb";

    public static IServiceCollection AddPersistence(
       this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<EventSerializer>();
        services.AddSingleton<CustomSaveChangesInterceptor>();

        services.AddDbContext<ShoppingCartDbContext>((serviceProvider, optionsBuilder) =>
        {
            // Resolved when the first DbContext is created, so hosts and tests can still override configuration.
            optionsBuilder.UseNpgsql(configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured."));
            optionsBuilder.AddInterceptors(serviceProvider.GetRequiredService<CustomSaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventStore, EventStore>();
        services.AddScoped<ICartWriteRepository, CartWriteRepository>();
        services.AddScoped<ICartReadRepository, CartReadRepository>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<OutboxSignal>();
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    /// <summary>Applies pending EF Core migrations.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
