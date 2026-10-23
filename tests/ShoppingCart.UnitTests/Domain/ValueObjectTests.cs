using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void Ids_reject_empty_guids()
    {
        Assert.Throws<DomainException>(() => CartId.Of(Guid.Empty));
        Assert.Throws<DomainException>(() => CartItemId.Of(Guid.Empty));
        Assert.Throws<DomainException>(() => VariantId.Of(Guid.Empty));
        Assert.Throws<DomainException>(() => MerchantId.Of(Guid.Empty));
        Assert.Throws<DomainException>(() => CartGuid.Of(Guid.Empty));
    }

    [Fact]
    public void Ids_with_the_same_value_are_equal()
    {
        var value = Guid.NewGuid();

        Assert.Equal(CartId.Of(value), CartId.Of(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Merchant_requires_a_name(string name)
    {
        Assert.Throws<DomainException>(() => new Merchant(MerchantId.Of(Guid.NewGuid()), name));
    }

    [Fact]
    public void Merchant_name_has_a_maximum_length()
    {
        Assert.Throws<DomainException>(() => new Merchant(MerchantId.Of(Guid.NewGuid()), new string('x', Merchant.MaxNameLength + 1)));
    }

    [Fact]
    public void Variant_price_cannot_be_negative()
    {
        Assert.Throws<DomainException>(() => new Variant(VariantId.Of(Guid.NewGuid()), -0.01m));
    }
}
