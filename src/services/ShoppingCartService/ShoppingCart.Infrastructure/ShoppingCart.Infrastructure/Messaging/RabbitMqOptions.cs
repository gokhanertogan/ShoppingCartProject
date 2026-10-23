using System.ComponentModel.DataAnnotations;

namespace ShoppingCart.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public const string ConnectionStringName = "RabbitMq";

    /// <summary>AMQP URI, e.g. <c>amqp://user:password@localhost:5672/</c>. Read from <c>ConnectionStrings:RabbitMq</c>.</summary>
    [Required]
    public string ConnectionString { get; set; } = default!;

    /// <summary>Durable topic exchange that all integration events are published to.</summary>
    [Required]
    public string ExchangeName { get; set; } = "shopping-cart.events";

    /// <summary>Prefix of the durable queues created by <c>SubscribeAsync</c>.</summary>
    [Required]
    public string QueuePrefix { get; set; } = "shopping-cart";

    public string ClientProvidedName { get; set; } = "shopping-cart-service";

    [Range(1, 1000)]
    public ushort PrefetchCount { get; set; } = 16;
}
