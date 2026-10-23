using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingCart.Persistence.Common.Data;
using ShoppingCart.Persistence.Common.Events.Interfaces;
using ShoppingCart.Persistence.Common.Serialization;

namespace ShoppingCart.Persistence.Common.Outbox;

/// <summary>
/// Dispatches committed domain events from the outbox to their <c>IDomainEventHandler</c>s
/// (at-least-once, in order). Rows are claimed with <c>FOR UPDATE SKIP LOCKED</c>, so several
/// instances of the service can run the processor at the same time without dispatching a message twice
/// concurrently.
/// </summary>
public sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    OutboxSignal signal,
    EventSerializer serializer,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const int MaxErrorLength = 2000;
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogBatchFailed(logger, exception);
            }

            // A full batch probably means more work is waiting: go again without sleeping.
            if (processed < _options.BatchSize)
            {
                try
                {
                    await signal.WaitAsync(_options.PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    /// <summary>Processes one batch and returns the number of messages that were dispatched.</summary>
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var messages = await dbContext.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedOn" IS NULL AND "Attempts" < {_options.MaxAttempts}
                ORDER BY "OccurredOn", "StreamVersion"
                LIMIT {_options.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var dispatched = 0;
        foreach (var message in messages)
        {
            try
            {
                var domainEvent = serializer.Deserialize(message.Type, message.Content);
                await dispatcher.DispatchAsync([domainEvent], cancellationToken);

                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
                dispatched++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Attempts++;
                message.Error = exception.ToString() is { Length: > MaxErrorLength } error ? error[..MaxErrorLength] : exception.ToString();
                LogDispatchFailed(logger, exception, message.Id, message.Type, message.Attempts);

                // Stop at the first failure to keep the remaining messages in order; retried on the next poll.
                break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (dispatched > 0)
        {
            LogDispatched(logger, dispatched);
        }

        return dispatched;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatched {Count} outbox message(s)")]
    private static partial void LogDispatched(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dispatching outbox message {MessageId} ({Type}) failed, attempt {Attempts}")]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception, Guid messageId, string type, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Processing the outbox failed")]
    private static partial void LogBatchFailed(ILogger logger, Exception exception);
}
