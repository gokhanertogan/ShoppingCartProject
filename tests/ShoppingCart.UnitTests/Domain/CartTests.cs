using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.ShoppingCart.Aggregates;
using ShoppingCart.Domain.ShoppingCart.Events;
using ShoppingCart.Domain.ShoppingCart.Exceptions;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.UnitTests.Domain;

public class CartTests
{
    [Fact]
    public void Create_raises_CartCreatedEvent_and_starts_at_version_1()
    {
        var cartGuid = CartGuid.Of(Guid.NewGuid());

        var cart = Cart.Create(cartGuid);

        var created = Assert.IsType<CartCreatedEvent>(Assert.Single(cart.DomainEvents));
        Assert.Equal(cart.Id, created.CartId);
        Assert.Equal(cartGuid, created.CartGuid);
        Assert.Equal(1, cart.Version);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_adds_a_line_and_raises_CartItemAddedEvent()
    {
        var cart = TestData.NewCart();
        var variant = TestData.Variant(12.5m);
        var merchant = TestData.Merchant();

        var item = cart.AddItem(variant, merchant, 2);

        Assert.Equal(2, item.Quantity);
        Assert.Equal(25m, cart.TotalPrice);
        var added = Assert.IsType<CartItemAddedEvent>(cart.DomainEvents[^1]);
        Assert.Equal(item.Id, added.CartItemId);
        Assert.Equal(variant.Id, added.VariantId);
        Assert.Equal(merchant.Id, added.MerchantId);
        Assert.Equal(2, cart.Version);
    }

    [Fact]
    public void AddItem_with_same_variant_and_merchant_increases_the_existing_line()
    {
        var cart = TestData.NewCart();
        var variantId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        cart.AddItem(TestData.Variant(10m, variantId), TestData.Merchant(id: merchantId), 2);
        cart.AddItem(TestData.Variant(10m, variantId), TestData.Merchant(id: merchantId), 3);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
        var updated = Assert.IsType<CartItemUpdatedEvent>(cart.DomainEvents[^1]);
        Assert.Equal(5, updated.Quantity);
    }

    [Fact]
    public void AddItem_with_same_variant_from_another_merchant_creates_a_new_line()
    {
        var cart = TestData.NewCart();
        var variantId = Guid.NewGuid();

        cart.AddItem(TestData.Variant(10m, variantId), TestData.Merchant("A"), 1);
        cart.AddItem(TestData.Variant(10m, variantId), TestData.Merchant("B"), 1);

        Assert.Equal(2, cart.Items.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Cart.MaxItemQuantity + 1)]
    public void AddItem_rejects_invalid_quantities(int quantity)
    {
        var cart = TestData.NewCart();

        Assert.Throws<CartException>(() => cart.AddItem(TestData.Variant(), TestData.Merchant(), quantity));
    }

    [Fact]
    public void AddItem_rejects_a_merge_that_exceeds_the_maximum_quantity()
    {
        var cart = TestData.NewCart();
        var variantId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        cart.AddItem(TestData.Variant(1m, variantId), TestData.Merchant(id: merchantId), Cart.MaxItemQuantity);

        Assert.Throws<CartException>(() => cart.AddItem(TestData.Variant(1m, variantId), TestData.Merchant(id: merchantId), 1));
        Assert.Equal(Cart.MaxItemQuantity, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void UpdateItemQuantity_sets_the_quantity_and_raises_CartItemUpdatedEvent()
    {
        var cart = TestData.NewCart();
        var item = cart.AddItem(TestData.Variant(3m), TestData.Merchant(), 1);

        cart.UpdateItemQuantity(item.Id, 4);

        Assert.Equal(4, item.Quantity);
        Assert.Equal(12m, cart.TotalPrice);
        Assert.IsType<CartItemUpdatedEvent>(cart.DomainEvents[^1]);
    }

    [Fact]
    public void UpdateItemQuantity_to_the_same_value_raises_no_event()
    {
        var cart = TestData.NewCart();
        var item = cart.AddItem(TestData.Variant(), TestData.Merchant(), 2);
        var version = cart.Version;

        cart.UpdateItemQuantity(item.Id, 2);

        Assert.Equal(version, cart.Version);
    }

    [Fact]
    public void Operations_on_an_unknown_item_throw_NotFoundException()
    {
        var cart = TestData.NewCart();
        var unknown = CartItemId.Of(Guid.NewGuid());

        Assert.Throws<NotFoundException>(() => cart.UpdateItemQuantity(unknown, 1));
        Assert.Throws<NotFoundException>(() => cart.RemoveItem(unknown));
    }

    [Fact]
    public void RemoveItem_removes_the_line_and_raises_CartItemRemovedEvent()
    {
        var cart = TestData.NewCart();
        var item = cart.AddItem(TestData.Variant(), TestData.Merchant(), 1);

        cart.RemoveItem(item.Id);

        Assert.Empty(cart.Items);
        var removed = Assert.IsType<CartItemRemovedEvent>(cart.DomainEvents[^1]);
        Assert.Equal(item.Id, removed.CartItemId);
    }

    [Fact]
    public void Clear_removes_all_lines_once()
    {
        var cart = TestData.NewCart();
        cart.AddItem(TestData.Variant(), TestData.Merchant("A"), 1);
        cart.AddItem(TestData.Variant(), TestData.Merchant("B"), 1);

        cart.Clear();
        var versionAfterClear = cart.Version;
        cart.Clear();

        Assert.Empty(cart.Items);
        var cleared = Assert.IsType<CartClearedEvent>(cart.DomainEvents[^1]);
        Assert.Equal(2, cleared.RemovedItemCount);
        Assert.Equal(versionAfterClear, cart.Version);
    }

    [Fact]
    public void ClearDomainEvents_returns_and_removes_pending_events_but_keeps_the_version()
    {
        var cart = TestData.NewCart();
        cart.AddItem(TestData.Variant(), TestData.Merchant(), 1);

        var events = cart.ClearDomainEvents();

        Assert.Equal(2, events.Length);
        Assert.Empty(cart.DomainEvents);
        Assert.Equal(2, cart.Version);
    }
}
