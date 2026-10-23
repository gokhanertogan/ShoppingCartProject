using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShoppingCart.Infrastructure.Messaging;

/// <summary>
/// Declares the exchange at startup. If RabbitMQ is not reachable yet, startup continues: the bus
/// connects on first use, the outbox keeps undelivered events, and /health/ready reports the outage.
/// </summary>
public sealed partial class RabbitMqTopologyInitializer(RabbitMqMessageBus messageBus, ILogger<RabbitMqTopologyInitializer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await messageBus.InitializeAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogInitializationFailed(logger, exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not declare the RabbitMQ topology at startup; it will be declared on first publish")]
    private static partial void LogInitializationFailed(ILogger logger, Exception exception);
}
