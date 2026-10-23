namespace ShoppingCart.Domain.Common;

/// <summary>
/// Marker for Guid-backed strongly typed ids (CartId, CartItemId, ...). By convention, each
/// implementation exposes a static <c>Of(Guid)</c> factory that validates the value.
/// </summary>
public interface IStronglyTypedId
{
    Guid Value { get; }
}
