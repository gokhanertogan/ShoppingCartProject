namespace ShoppingCart.Domain.Exceptions;

/// <summary>A business rule of the domain was violated.</summary>
public class DomainException(string message) : Exception(message)
{

}
