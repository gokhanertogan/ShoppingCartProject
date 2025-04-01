using ShoppingCart.Application.Cart.Commands.Common;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Utils;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Commands;

public record CreateCartCommand : ICommand<Domain.ShoppingCart.Aggregates.Cart>
{
    public Guid CartGuid { get; set; }
    public List<CartItemRequest> Items { get; set; } = [];
}


//TODO should be create cart item model by merchants
public record CartItemRequest(VariantItemRequest Variant, MerchantItemRequest Merchant, int Quantity)
{
    public VariantItemRequest Variant { get; set; } = Variant;
    public MerchantItemRequest Merchant { get; set; } = Merchant;
    public int Quantity { get; set; } = Quantity;
}

public record VariantItemRequest(Guid Id, Decimal Price)
{
    public Guid Id { get; set; } = Id;
    public decimal Price { get; set; } = Price;
}

public record MerchantItemRequest(Guid Id, string Name)
{
    public Guid Id { get; set; } = Id;
    public string Name { get; set; } = Name;
}

public class CreateCartCommandCommandHandler(ICartWriteRepository cartWriteRepository) : ICommandHandler<CreateCartCommand, Domain.ShoppingCart.Aggregates.Cart>
{
    private readonly ICartWriteRepository _cartWriteRepository = cartWriteRepository;
    public async Task<Result<Domain.ShoppingCart.Aggregates.Cart>> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cartGuid = CartGuid.Of(request.CartGuid);
            var cart = Domain.ShoppingCart.Aggregates.Cart.Create(cartGuid);
            foreach (var item in request.Items)
            {
                var variantId = VariantId.Of(item.Variant.Id);
                Variant variant = new(variantId, item.Variant.Price);
                var merchantId = MerchantId.Of(item.Merchant.Id);
                Merchant merchant = new(merchantId, item.Merchant.Name);
                cart.AddItem(variant, merchant, item.Quantity);
            }

            await _cartWriteRepository.AddAsync(cart, CancellationToken.None);   
            await _cartWriteRepository.SaveAsync(CancellationToken.None);         

            return Result<Domain.ShoppingCart.Aggregates.Cart>.Success(cart, 201);
        }
        catch (Exception exception)
        {
            return Result<Domain.ShoppingCart.Aggregates.Cart>.Fail(exception.ToString(), 500);
            //TODO should be logged
        }
    }
}
