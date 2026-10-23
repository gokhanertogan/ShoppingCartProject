using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Application.Cart.Commands;
using ShoppingCart.Application.Cart.Dtos;
using ShoppingCart.Application.Cart.Events;
using ShoppingCart.Application.Common.Messaging;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.ShoppingCart.Entities;
using ShoppingCart.Domain.ShoppingCart.ValueObjects;
using ShoppingCart.Persistence.Common.Data;

namespace ShoppingCart.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class CartApiTests(ShoppingCartApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static CartItemRequest Item(Guid variantId, Guid merchantId, decimal price, int quantity, string merchant = "Acme") =>
        new(new VariantItemRequest(variantId, price), new MerchantItemRequest(merchantId, merchant), quantity);

    private async Task<CartDto> CreateCartAsync(params CartItemRequest[] items)
    {
        var response = await _client.PostAsJsonAsync("/api/carts", new { cartGuid = Guid.NewGuid(), items }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CartDto>(Ct))!;
    }

    [Fact]
    public async Task Full_cart_flow_persists_state_appends_events_and_publishes_integration_events()
    {
        // Subscribe before acting so the queues are bound when the outbox publishes.
        var bus = factory.Services.GetRequiredService<IMessageBus>();
        var received = new ConcurrentQueue<object>();
        await bus.SubscribeAsync<CartCreatedIntegrationEvent>(e => { received.Enqueue(e); return Task.CompletedTask; }, Ct);
        await bus.SubscribeAsync<CartItemAddedIntegrationEvent>(e => { received.Enqueue(e); return Task.CompletedTask; }, Ct);
        await bus.SubscribeAsync<CartItemQuantityChangedIntegrationEvent>(e => { received.Enqueue(e); return Task.CompletedTask; }, Ct);
        await bus.SubscribeAsync<CartItemRemovedIntegrationEvent>(e => { received.Enqueue(e); return Task.CompletedTask; }, Ct);
        await bus.SubscribeAsync<CartClearedIntegrationEvent>(e => { received.Enqueue(e); return Task.CompletedTask; }, Ct);

        var variantA = Guid.NewGuid();
        var merchant = Guid.NewGuid();

        // Create: two requests for the same variant and merchant are merged into one line.
        var createResponse = await _client.PostAsJsonAsync("/api/carts", new
        {
            cartGuid = Guid.NewGuid(),
            items = new[] { Item(variantA, merchant, 10.50m, 2), Item(variantA, merchant, 10.50m, 1) },
        }, Ct);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var cart = (await createResponse.Content.ReadFromJsonAsync<CartDto>(Ct))!;
        Assert.Equal($"/api/carts/{cart.Id}", createResponse.Headers.Location?.OriginalString);
        Assert.Equal(3, Assert.Single(cart.Items).Quantity);
        Assert.Equal(31.50m, cart.TotalPrice);

        // Add a second line.
        var addResponse = await _client.PostAsJsonAsync($"/api/carts/{cart.Id}/items", Item(Guid.NewGuid(), merchant, 4.25m, 2), Ct);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        cart = (await addResponse.Content.ReadFromJsonAsync<CartDto>(Ct))!;
        Assert.Equal(2, cart.Items.Count);
        var secondLine = cart.Items.Single(i => i.VariantId != variantA);

        // Update its quantity.
        var updateResponse = await _client.PutAsJsonAsync($"/api/carts/{cart.Id}/items/{secondLine.Id}", new { quantity = 5 }, Ct);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Read it back from the database.
        cart = (await _client.GetFromJsonAsync<CartDto>($"/api/carts/{cart.Id}", Ct))!;
        Assert.Equal(5, cart.Items.Single(i => i.Id == secondLine.Id).Quantity);
        Assert.Equal(31.50m + 21.25m, cart.TotalPrice);

        // Remove the line, then clear the cart.
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/carts/{cart.Id}/items/{secondLine.Id}", Ct)).StatusCode);
        var clearResponse = await _client.DeleteAsync($"/api/carts/{cart.Id}/items", Ct);
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);
        cart = (await clearResponse.Content.ReadFromJsonAsync<CartDto>(Ct))!;
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.TotalPrice);

        // Event store: one stream per cart, versions 1..n in order, matching the cart version.
        var history = (await _client.GetFromJsonAsync<List<CartEventDto>>($"/api/carts/{cart.Id}/events", Ct))!;
        Assert.Equal(
            ["CartCreatedEvent", "CartItemAddedEvent", "CartItemUpdatedEvent", "CartItemAddedEvent", "CartItemUpdatedEvent", "CartItemRemovedEvent", "CartClearedEvent"],
            history.Select(e => e.EventType));
        Assert.Equal(Enumerable.Range(1, history.Count).Select(v => (long)v), history.Select(e => e.Version));
        Assert.Equal(history.Count, cart.Version);

        // Outbox -> domain event handler -> RabbitMQ: every event arrives once, with the stored event id.
        var cartMessages = await Eventually.GetAsync(
            () => Task.FromResult(received.Where(m => BelongsTo(m, cart.Id)).ToList()),
            messages => messages.Count >= history.Count);
        Assert.Equal(history.Count, cartMessages.Count);
        Assert.Equal(history.Select(e => e.EventId).Order(), cartMessages.Select(EventIdOf).Order());

        // All outbox messages of this cart were marked as processed.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();
        var eventIds = history.Select(e => e.EventId).ToList();
        var unprocessed = await Eventually.GetAsync(
            () => db.OutboxMessages.AsNoTracking().CountAsync(m => eventIds.Contains(m.Id) && m.ProcessedOn == null, Ct),
            count => count == 0);
        Assert.Equal(0, unprocessed);

        // ...and they were dispatched in stream order.
        var dispatchOrder = await db.OutboxMessages.AsNoTracking()
            .Where(m => eventIds.Contains(m.Id))
            .OrderBy(m => m.ProcessedOn)
            .Select(m => m.Id)
            .ToListAsync(Ct);
        Assert.Equal(eventIds, dispatchOrder);
    }

    private static bool BelongsTo(object message, Guid cartId) => message switch
    {
        CartCreatedIntegrationEvent e => e.CartId == cartId,
        CartItemAddedIntegrationEvent e => e.CartId == cartId,
        CartItemQuantityChangedIntegrationEvent e => e.CartId == cartId,
        CartItemRemovedIntegrationEvent e => e.CartId == cartId,
        CartClearedIntegrationEvent e => e.CartId == cartId,
        _ => false,
    };

    private static Guid EventIdOf(object message) => ((ShoppingCart.Application.Common.Events.IIntegrationEvent)message).EventId;

    [Fact]
    public async Task Invalid_quantity_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/carts", new
        {
            cartGuid = Guid.NewGuid(),
            items = new[] { Item(Guid.NewGuid(), Guid.NewGuid(), 1m, 0) },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(400, problem?.Status);
        Assert.Contains("quantity", problem?.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Empty_cart_guid_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/carts", new { cartGuid = Guid.Empty }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_body_returns_400_problem_details()
    {
        using var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/carts", content, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_cart_and_unknown_item_return_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/carts/{Guid.NewGuid()}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/carts/{Guid.NewGuid()}/events", Ct)).StatusCode);

        var cart = await CreateCartAsync();
        var response = await _client.PutAsJsonAsync($"/api/carts/{cart.Id}/items/{Guid.NewGuid()}", new { quantity = 1 }, Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Concurrent_writers_of_the_same_cart_conflict()
    {
        var cart = await CreateCartAsync();
        var cartId = CartId.Of(cart.Id);

        await using var scope1 = factory.Services.CreateAsyncScope();
        await using var scope2 = factory.Services.CreateAsyncScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();
        var copy1 = await db1.Carts.Include(c => c.Items).SingleAsync(c => c.Id == cartId, Ct);
        var copy2 = await db2.Carts.Include(c => c.Items).SingleAsync(c => c.Id == cartId, Ct);

        copy1.AddItem(new Variant(VariantId.Of(Guid.NewGuid()), 1m), new Merchant(MerchantId.Of(Guid.NewGuid()), "A"), 1);
        copy2.AddItem(new Variant(VariantId.Of(Guid.NewGuid()), 2m), new Merchant(MerchantId.Of(Guid.NewGuid()), "B"), 1);

        await scope1.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => scope2.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Ct));

        var stored = (await _client.GetFromJsonAsync<CartDto>($"/api/carts/{cart.Id}", Ct))!;
        Assert.Single(stored.Items);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Health_and_openapi_endpoints_respond()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/live", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/ready", Ct)).StatusCode);

        var openApi = await _client.GetStringAsync("/openapi/v1.json", Ct);
        Assert.Contains("/api/carts/{cartId}/items/{itemId}", openApi, StringComparison.Ordinal);
    }
}
