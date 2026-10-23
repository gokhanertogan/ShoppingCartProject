using ShoppingCart.Application.Cart.Commands.Common;
using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Application.Common.Utils;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Commands;

public record CreateCartCommand : ICommand<CartDto>
{
    public Guid CartGuid { get; init; }
    public List<CartItemRequest> Items { get; init; } = [];
}

//TODO should be create cart item model by merchants
public record CartItemRequest(VariantItemRequest Variant, MerchantItemRequest Merchant, int Quantity);

public record VariantItemRequest(Guid Id, decimal Price);

public record MerchantItemRequest(Guid Id, string Name);

internal static class CartItemRequestExtensions
{
    public static (Variant Variant, Merchant Merchant) ToDomain(this CartItemRequest item)
    {
        if (item.Variant is null || item.Merchant is null)
        {
            throw new Domain.Exceptions.DomainException("variant and merchant are required for a cart item");
        }

        var variant = new Variant(VariantId.Of(item.Variant.Id), item.Variant.Price);
        var merchant = new Merchant(MerchantId.Of(item.Merchant.Id), item.Merchant.Name);
        return (variant, merchant);
    }
}

public class CreateCartCommandHandler(ICartWriteRepository cartWriteRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCartCommand, CartDto>
{
    private readonly ICartWriteRepository _cartWriteRepository = cartWriteRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Task<Result<CartDto>> Handle(CreateCartCommand request, CancellationToken cancellationToken) =>
        DomainOperation.ExecuteAsync(async () =>
        {
            var cart = Domain.ShoppingCart.Aggregates.Cart.Create(CartGuid.Of(request.CartGuid));
            foreach (var item in request.Items ?? [])
            {
                var (variant, merchant) = item.ToDomain();
                cart.AddItem(variant, merchant, item.Quantity);
            }

            await _cartWriteRepository.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CartDto>.Success(cart.ToDto(), 201);
        });
}
