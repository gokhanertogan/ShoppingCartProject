namespace ShoppingCart.Application.Cart.Dtos;

public sealed record CartDto(
    Guid Id,
    Guid CartGuid,
    long Version,
    IReadOnlyList<CartItemDto> Items,
    int TotalQuantity,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime? LastModified);

public sealed record CartItemDto(
    Guid Id,
    Guid VariantId,
    decimal UnitPrice,
    Guid MerchantId,
    string MerchantName,
    int Quantity,
    decimal TotalPrice);

public sealed record CartEventDto(
    long Version,
    string EventType,
    Guid EventId,
    DateTime OccurredOn,
    DateTime RecordedAt,
    object Data);

public static class CartMappings
{
    public static CartDto ToDto(this Domain.ShoppingCart.Aggregates.Cart cart) => new(
        cart.Id.Value,
        cart.CartGuid.Value,
        cart.Version,
        [.. cart.Items.Select(item => new CartItemDto(
            item.Id.Value,
            item.Variant.Id.Value,
            item.Variant.Price,
            item.Merchant.Id.Value,
            item.Merchant.Name,
            item.Quantity,
            item.GetTotalPrice()))],
        cart.Items.Sum(item => item.Quantity),
        cart.TotalPrice,
        cart.CreatedAt,
        cart.LastModified);
}
