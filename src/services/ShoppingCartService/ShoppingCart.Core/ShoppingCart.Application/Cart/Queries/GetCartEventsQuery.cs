using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Queries.Common;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Application.Common.Utils;

namespace ShoppingCart.Application.Cart.Queries;

/// <summary>Reads the event stream (history) of a cart from the event store.</summary>
public record GetCartEventsQuery(Guid CartId) : IQuery<IReadOnlyList<CartEventDto>>;

public class GetCartEventsQueryHandler(IEventStore eventStore) : IQueryHandler<GetCartEventsQuery, IReadOnlyList<CartEventDto>>
{
    public async Task<Result<IReadOnlyList<CartEventDto>>> Handle(GetCartEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await eventStore.GetEventsAsync(request.CartId, cancellationToken);
        if (events.Count == 0)
        {
            return CartErrors.NotFound<IReadOnlyList<CartEventDto>>(request.CartId);
        }

        IReadOnlyList<CartEventDto> history = [.. events.Select(recorded => new CartEventDto(
            recorded.Version,
            recorded.Event.EventType,
            recorded.Event.EventId,
            recorded.Event.OccurredOn,
            recorded.RecordedAt,
            recorded.Event))];

        return Result<IReadOnlyList<CartEventDto>>.Success(history, 200);
    }
}
