using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Queries.Common;
using ShoppingCart.Application.Cart.Repositories;
using ShoppingCart.Application.Common.Utils;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;

namespace ShoppingCart.Application.Cart.Queries;

public record GetCartQuery(Guid CartId) : IQuery<CartDto>;

public class GetCartQueryHandler(ICartReadRepository cartReadRepository) : IQueryHandler<GetCartQuery, CartDto>
{
    public Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken) =>
        DomainOperation.ExecuteAsync(async () =>
        {
            var cart = await cartReadRepository.FindWithItemsAsync(CartId.Of(request.CartId), cancellationToken);
            return cart is null
                ? CartErrors.NotFound<CartDto>(request.CartId)
                : Result<CartDto>.Success(cart.ToDto(), 200);
        });
}
