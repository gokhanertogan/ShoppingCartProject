namespace ShoppingCart.Domain.Exceptions;

/// <summary>An entity referenced by a domain operation does not exist.</summary>
public class NotFoundException(string message) : DomainException(message)
{

}
