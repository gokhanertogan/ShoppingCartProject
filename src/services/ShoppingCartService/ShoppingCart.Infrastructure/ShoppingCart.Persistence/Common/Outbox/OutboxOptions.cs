namespace ShoppingCart.Persistence.Common.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>How often the processor polls when it has not been signalled.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 50;

    /// <summary>A message that failed this many times is no longer retried and needs manual attention.</summary>
    public int MaxAttempts { get; set; } = 10;
}
