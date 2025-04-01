using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.CartItem.Repositories;
using ShoppingCart.Persistence.Cart.Repositories;
using ShoppingCart.Persistence.Common.Data;

namespace ShoppingCart.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        return services;
    }

    public static IServiceCollection AddPersistence(
       this IServiceCollection services, IConfiguration configuration)
    {

        services.AddDbContext<ShoppingCartDbContext>((serviceProvider, optionsBuilder) =>
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BasketDB;Username=sa;Password=Ge12345*");
            optionsBuilder.AddInterceptors(serviceProvider.GetRequiredService<CustomSaveChangesInterceptor>());
        });

        services.AddScoped<CustomSaveChangesInterceptor>();

        services.AddScoped<ICartWriteRepository, CartWriteRepository>();
        return services;
    }
}