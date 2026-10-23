using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Application.Common.Utils;

/// <summary>Turns domain rule violations into failed results (404 for missing entities, 400 otherwise).</summary>
public static class DomainOperation
{
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation)
    {
        try
        {
            return await operation();
        }
        catch (NotFoundException exception)
        {
            return Result<T>.Fail(exception.Message, 404);
        }
        catch (DomainException exception)
        {
            return Result<T>.Fail(exception.Message, 400);
        }
    }
}
