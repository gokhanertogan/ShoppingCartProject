using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShoppingCart.Infrastructure.Messaging;

public sealed class RabbitMqHealthCheck(RabbitMqMessageBus messageBus) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await messageBus.IsConnectedAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, "The RabbitMQ connection is closed.");
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Cannot connect to RabbitMQ.", exception);
        }
    }
}
