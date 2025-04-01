namespace ShoppingCart.Application.Cart.Commands.Common;
 
using MediatR;
using ShoppingCart.Application.Common.Utils;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}