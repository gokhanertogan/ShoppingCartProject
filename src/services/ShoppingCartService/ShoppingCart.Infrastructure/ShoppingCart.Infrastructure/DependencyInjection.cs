using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Common.Messaging;
using ShoppingCart.Infrastructure.Messaging;

namespace ShoppingCart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Configure(options => options.ConnectionString = configuration.GetConnectionString(RabbitMqOptions.ConnectionStringName) ?? options.ConnectionString)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<RabbitMqMessageBus>();
        services.AddSingleton<IMessageBus>(serviceProvider => serviceProvider.GetRequiredService<RabbitMqMessageBus>());
        services.AddHostedService<RabbitMqTopologyInitializer>();

        return services;
    }
}
