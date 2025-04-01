using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Domain.Validations;

namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class Merchant : Entity<MerchantId>
{
    private Merchant() { }
    public MerchantId MerchantId { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public Merchant(MerchantId merchantId, string name)
    {
        DomainValidation.ValidateNotEmptyOrWhitespace(name, nameof(Name));

        Id = merchantId;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
}