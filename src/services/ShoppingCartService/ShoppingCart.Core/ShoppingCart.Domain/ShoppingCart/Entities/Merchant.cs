using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class Merchant : Entity<MerchantId>
{
    private Merchant() { }
    public MerchantId MerchantId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
}