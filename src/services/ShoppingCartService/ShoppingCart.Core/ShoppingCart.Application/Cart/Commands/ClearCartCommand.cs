using ShoppingCart.Application.Cart.Commands.Common;
using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Application.Common.Utils;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Commands;

public record ClearCartCommand(Guid CartId) : ICommand<CartDto>;

public class ClearCartCommandHandler(ICartWriteRepository cartWriteRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ClearCartCommand, CartDto>
{
    public Task<Result<CartDto>> Handle(ClearCartCommand request, CancellationToken cancellationToken) =>
        DomainOperation.ExecuteAsync(async () =>
        {
            var cart = await cartWriteRepository.GetByIdAsync(CartId.Of(request.CartId), cancellationToken);
            if (cart is null)
            {
                return CartErrors.NotFound<CartDto>(request.CartId);
            }

            cart.Clear();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CartDto>.Success(cart.ToDto(), 200);
        });
}
