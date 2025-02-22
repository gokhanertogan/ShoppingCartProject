using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.ShoppingCart.Exceptions;

public sealed class CartException(string message) : DomainException(message)
{
    
}