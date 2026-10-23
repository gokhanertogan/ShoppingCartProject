using MediatR;
using ShoppingCart.Application.Common.Utils;

namespace ShoppingCart.Application.Cart.Queries.Common;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
