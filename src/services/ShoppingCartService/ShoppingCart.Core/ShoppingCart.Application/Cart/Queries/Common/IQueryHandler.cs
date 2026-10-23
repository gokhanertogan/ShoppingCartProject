using MediatR;
using ShoppingCart.Application.Common.Utils;

namespace ShoppingCart.Application.Cart.Queries.Common;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
