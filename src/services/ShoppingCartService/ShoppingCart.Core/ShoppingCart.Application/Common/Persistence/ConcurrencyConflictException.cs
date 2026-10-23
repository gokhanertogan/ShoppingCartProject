namespace ShoppingCart.Application.Common.Persistence;

/// <summary>
/// Another writer changed the same aggregate (or appended to its event stream) after it was loaded.
/// The caller can reload the aggregate and retry.
/// </summary>
public sealed class ConcurrencyConflictException(string message, Exception innerException) : Exception(message, innerException);
