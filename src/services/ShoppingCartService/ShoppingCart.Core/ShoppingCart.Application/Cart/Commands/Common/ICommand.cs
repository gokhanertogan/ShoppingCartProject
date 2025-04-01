using MediatR;
using ShoppingCart.Application.Common.Utils;

namespace ShoppingCart.Application.Cart.Commands.Common;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
