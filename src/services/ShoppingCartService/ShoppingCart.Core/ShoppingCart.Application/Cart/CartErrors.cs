using ShoppingCart.Application.Common.Utils;

namespace ShoppingCart.Application.Cart;

internal static class CartErrors
{
    public static Result<T> NotFound<T>(Guid cartId) => Result<T>.Fail($"Cart '{cartId}' was not found.", 404);
}
