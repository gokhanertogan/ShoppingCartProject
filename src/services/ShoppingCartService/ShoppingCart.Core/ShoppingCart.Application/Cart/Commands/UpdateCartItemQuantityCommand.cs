using ShoppingCart.Application.Cart.Commands.Common;
using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Application.Common.Utils;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Commands;

public record UpdateCartItemQuantityCommand(Guid CartId, Guid CartItemId, int Quantity) : ICommand<CartDto>;

public class UpdateCartItemQuantityCommandHandler(ICartWriteRepository cartWriteRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCartItemQuantityCommand, CartDto>
{
    public Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken) =>
        DomainOperation.ExecuteAsync(async () =>
        {
            var cart = await cartWriteRepository.GetByIdAsync(CartId.Of(request.CartId), cancellationToken);
            if (cart is null)
            {
                return CartErrors.NotFound<CartDto>(request.CartId);
            }

            cart.UpdateItemQuantity(CartItemId.Of(request.CartItemId), request.Quantity);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CartDto>.Success(cart.ToDto(), 200);
        });
}
