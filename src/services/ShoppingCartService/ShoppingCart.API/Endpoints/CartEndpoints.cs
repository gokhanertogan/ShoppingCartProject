using MediatR;
using ShoppingCart.Application.Cart.Commands;
using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Queries;

namespace ShoppingCart.API.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var carts = app.MapGroup("/api/carts").WithTags("Carts");

        carts.MapPost("/", async (CreateCartRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new CreateCartCommand { CartGuid = request.CartGuid, Items = request.Items ?? [] }, cancellationToken);
                return result.ToHttpResult(cart => TypedResults.Created($"/api/carts/{cart.Id}", cart));
            })
            .WithName("CreateCart")
            .WithSummary("Creates a cart, optionally with initial items")
            .Produces<CartDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        carts.MapGet("/{cartId:guid}", async (Guid cartId, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new GetCartQuery(cartId), cancellationToken)).ToHttpResult())
            .WithName("GetCart")
            .WithSummary("Gets a cart with its items and totals")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        carts.MapGet("/{cartId:guid}/events", async (Guid cartId, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new GetCartEventsQuery(cartId), cancellationToken)).ToHttpResult())
            .WithName("GetCartEvents")
            .WithSummary("Gets the event stream (history) of a cart from the event store")
            .Produces<IReadOnlyList<CartEventDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        carts.MapPost("/{cartId:guid}/items", async (Guid cartId, CartItemRequest request, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new AddCartItemCommand(cartId, request), cancellationToken)).ToHttpResult())
            .WithName("AddCartItem")
            .WithSummary("Adds an item; the same variant from the same merchant increases the existing line")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        carts.MapPut("/{cartId:guid}/items/{itemId:guid}", async (Guid cartId, Guid itemId, UpdateCartItemRequest request, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new UpdateCartItemQuantityCommand(cartId, itemId, request.Quantity), cancellationToken)).ToHttpResult())
            .WithName("UpdateCartItemQuantity")
            .WithSummary("Sets the quantity of a cart line")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        carts.MapDelete("/{cartId:guid}/items/{itemId:guid}", async (Guid cartId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new RemoveCartItemCommand(cartId, itemId), cancellationToken)).ToHttpResult())
            .WithName("RemoveCartItem")
            .WithSummary("Removes a cart line")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        carts.MapDelete("/{cartId:guid}/items", async (Guid cartId, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new ClearCartCommand(cartId), cancellationToken)).ToHttpResult())
            .WithName("ClearCart")
            .WithSummary("Removes all items from a cart")
            .Produces<CartDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

public sealed record CreateCartRequest(Guid CartGuid, List<CartItemRequest>? Items);

public sealed record UpdateCartItemRequest(int Quantity);
