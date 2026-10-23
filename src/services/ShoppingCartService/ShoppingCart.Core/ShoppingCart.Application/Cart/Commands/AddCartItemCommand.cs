using ShoppingCart.Application.Cart.Commands.Common;
using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Application.Common.Utils;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Commands;

public record AddCartItemCommand(Guid CartId, CartItemRequest Item) : ICommand<CartDto>;

public class AddCartItemCommandHandler(ICartWriteRepository cartWriteRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddCartItemCommand, CartDto>
{
    public Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken) =>
        DomainOperation.ExecuteAsync(async () =>
        {
            var cart = await cartWriteRepository.GetByIdAsync(CartId.Of(request.CartId), cancellationToken);
            if (cart is null)
            {
                return CartErrors.NotFound<CartDto>(request.CartId);
            }

            var (variant, merchant) = request.Item.ToDomain();
            cart.AddItem(variant, merchant, request.Item.Quantity);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CartDto>.Success(cart.ToDto(), 200);
        });
}
