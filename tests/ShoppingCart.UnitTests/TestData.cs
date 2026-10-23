using ShoppingCart.Domain.ShoppingCart.Aggregates;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.UnitTests;

internal static class TestData
{
    public static Cart NewCart() => Cart.Create(CartGuid.Of(Guid.NewGuid()));

    public static Variant Variant(decimal price = 10m, Guid? id = null) => new(VariantId.Of(id ?? Guid.NewGuid()), price);

    public static Merchant Merchant(string name = "Acme", Guid? id = null) => new(MerchantId.Of(id ?? Guid.NewGuid()), name);
}
