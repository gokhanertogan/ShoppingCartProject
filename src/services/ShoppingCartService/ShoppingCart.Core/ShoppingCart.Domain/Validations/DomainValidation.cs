using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Domain.Validations;

public static class DomainValidation
{
    public static void ValidateNotEmptyOrWhitespace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{parameterName} cannot be null, empty, or whitespace.");
        }
    }

    public static void ValidateMinLength(string parameterName, string value, int minLength)
    {
        if (value.Length < minLength)
        {
            throw new DomainException($"{parameterName} must be at least {minLength} characters long.");
        }
    }

    public static void ValidateMaxLength(string parameterName, string value, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new DomainException($"{parameterName} must be no more than {maxLength} characters long");
        }
    }

    public static void ValidateMin(string parameterName, decimal value, int minValue)
    {
        if (value < minValue)
        {
            throw new DomainException($"{parameterName} must be at least {minValue}");
        }
    }

    public static void ThrowIfNull(object value, string parameterName)
    {
        if (value == default(object))
        {
            throw new DomainException($"{parameterName} not found");
        }
    }
}