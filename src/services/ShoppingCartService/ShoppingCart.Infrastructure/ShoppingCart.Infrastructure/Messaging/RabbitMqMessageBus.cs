using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShoppingCart.Application.Common.Events;
using ShoppingCart.Application.Common.Messaging;

namespace ShoppingCart.Infrastructure.Messaging;

/// <summary>
/// <see cref="IMessageBus"/> on RabbitMQ (RabbitMQ.Client 7). Messages are JSON, published as persistent
/// messages to a durable topic exchange with the message type name as routing key, and each publish
/// waits for the broker's publisher confirm. One connection is shared by the process; publishing uses
/// a single confirm channel and each subscription gets its own channel.
/// </summary>
public sealed partial class RabbitMqMessageBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessageBus> logger)
    : IMessageBus, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private readonly List<IChannel> _consumerChannels = [];
    private IConnection? _connection;
    private IChannel? _publishChannel;

    public static string GetRoutingKey(Type messageType) => messageType.Name;

    public string GetQueueName(Type messageType) => $"{_options.QueuePrefix}.{messageType.Name}";

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();
        var body = JsonSerializer.SerializeToUtf8Bytes(message, messageType, SerializerOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = messageType.Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            // The integration event id is stable across redeliveries, so consumers can de-duplicate on it.
            MessageId = message is IIntegrationEvent integrationEvent ? integrationEvent.EventId.ToString() : Guid.NewGuid().ToString(),
        };

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetPublishChannelAsync(cancellationToken);
            // With publisher confirmation tracking enabled, this completes once the broker has confirmed
            // the message and throws if it was nacked.
            await channel.BasicPublishAsync(_options.ExchangeName, GetRoutingKey(messageType), mandatory: false, properties, body, cancellationToken);
        }
        finally
        {
            _publishLock.Release();
        }

        LogPublished(logger, messageType.Name, properties.MessageId, _options.ExchangeName);
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        lock (_consumerChannels)
        {
            _consumerChannels.Add(channel);
        }

        var queueName = GetQueueName(typeof(T));
        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName, _options.ExchangeName, GetRoutingKey(typeof(T)), cancellationToken: cancellationToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(delivery.Body.Span, SerializerOptions)
                    ?? throw new JsonException($"Message {delivery.BasicProperties.MessageId} has an empty body.");
                await handler(message);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                LogConsumeFailed(logger, exception, queueName, delivery.BasicProperties.MessageId);
                // Not requeued, to avoid a hot redelivery loop; attach a dead-letter exchange to the queue to keep these.
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken);
        LogSubscribed(logger, queueName, typeof(T).Name);
    }

    /// <summary>Connects and declares the exchange, so consumers can bind to it before the first publish.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            await GetPublishChannelAsync(cancellationToken);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    /// <summary>Opens the connection if needed and reports whether it is open. Used by the health check.</summary>
    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return connection.IsOpen;
    }

    private async Task<IChannel> GetPublishChannelAsync(CancellationToken cancellationToken)
    {
        if (_publishChannel is { IsOpen: true })
        {
            return _publishChannel;
        }

        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        if (_publishChannel is not null)
        {
            await _publishChannel.DisposeAsync();
        }

        _publishChannel = channel;
        return channel;
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is null)
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_options.ConnectionString),
                    ClientProvidedName = _options.ClientProvidedName,
                    AutomaticRecoveryEnabled = true,
                    TopologyRecoveryEnabled = true,
                };
                _connection = await factory.CreateConnectionAsync(cancellationToken);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<IChannel> channels;
        lock (_consumerChannels)
        {
            channels = [.. _consumerChannels];
            _consumerChannels.Clear();
        }

        foreach (var channel in channels)
        {
            await channel.DisposeAsync();
        }

        if (_publishChannel is not null)
        {
            await _publishChannel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
        _publishLock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {MessageType} {MessageId} to exchange {Exchange}")]
    private static partial void LogPublished(ILogger logger, string messageType, string? messageId, string exchange);

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming {MessageType} from queue {Queue}")]
    private static partial void LogSubscribed(ILogger logger, string queue, string messageType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handling message {MessageId} from queue {Queue} failed; the message was rejected")]
    private static partial void LogConsumeFailed(ILogger logger, Exception exception, string queue, string? messageId);
}
