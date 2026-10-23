using ShoppingCart.Domain.Common;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Domain.Validations;

namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class Merchant : Entity<MerchantId>
{
    public const int MaxNameLength = 200;

    private Merchant() { }
    public MerchantId MerchantId { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public Merchant(MerchantId merchantId, string name)
    {
        DomainValidation.ValidateNotEmptyOrWhitespace(name, nameof(Name));
        DomainValidation.ValidateMaxLength(nameof(Name), name, MaxNameLength);

        Id = merchantId;
        MerchantId = merchantId;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
}