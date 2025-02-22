namespace ShoppingCart.Domain.ShoppingCart.Entities;

public class CartItem(Variant variant, Merchant merchant, int quantity)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Variant Variant { get; private set; } = variant;
    public Merchant Merchant { get; private set; } = merchant;
    public int Quantity { get; private set; } = quantity;

    public void IncreaseQuantity(int quantity)
    {
        Quantity += quantity;
    }

    public void DecreaseQuantity(int quantity)
    {
        Quantity -= quantity;
    }

    public decimal GetTotalPrice()
    {
        return Quantity * Variant.Price;
    }
}