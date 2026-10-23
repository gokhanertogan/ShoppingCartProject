using ShoppingCart.Application.Common.Utils;

namespace ShoppingCart.API.Endpoints;

public static class ResultExtensions
{
    /// <summary>
    /// Maps an application <see cref="Result{T}"/> to HTTP: the data on success, a ProblemDetails
    /// response with the result's status code and errors on failure.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccessful)
        {
            return onSuccess?.Invoke(result.Data) ?? TypedResults.Json(result.Data, statusCode: result.StatusCode);
        }

        return TypedResults.Problem(
            detail: string.Join(" ", result.Errors),
            statusCode: result.StatusCode,
            extensions: new Dictionary<string, object?> { ["errors"] = result.Errors });
    }
}
