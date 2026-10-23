namespace ShoppingCart.Persistence.Common.Outbox;

/// <summary>A domain event waiting to be dispatched to its handlers after commit.</summary>
public sealed class OutboxMessage
{
    /// <summary>The id of the domain event.</summary>
    public Guid Id { get; init; }
    public string Type { get; init; } = default!;
    public string Content { get; init; } = default!;
    public DateTime OccurredOn { get; init; }

    /// <summary>Version of the event in its aggregate's stream; orders events raised in the same instant.</summary>
    public long StreamVersion { get; init; }
    public DateTime? ProcessedOn { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}
