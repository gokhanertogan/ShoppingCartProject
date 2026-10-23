# ShoppingCartProject

A shopping cart microservice in .NET 10, built with Clean Architecture and DDD tactical patterns (aggregates, strongly typed IDs, domain events). It stores carts in PostgreSQL with EF Core, keeps an append-only event log of every cart, and publishes integration events to RabbitMQ through a transactional outbox.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791?logo=postgresql&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4-FF6600?logo=rabbitmq&logoColor=white)
![MediatR](https://img.shields.io/badge/MediatR-12.5-blue)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

## Overview

`ShoppingCartService` models a multi-merchant shopping cart. A **Cart** aggregate holds **CartItems**. Each item references a product **Variant** (with its price) and the **Merchant** that sells it. Adding the same variant from the same merchant again increases the quantity of the existing line instead of creating a new one. Every state change raises a domain event.

What the service does:

- **HTTP API** (ASP.NET Core minimal APIs): create a cart, get it, add / update / remove items, clear it, and read its event history. Errors are RFC 9457 ProblemDetails. Health checks and an OpenAPI document are included.
- **Event store**: every domain event is appended to the cart's stream in a PostgreSQL table, in the same transaction as the cart itself.
- **Transactional outbox**: the same events go to an outbox table; a background processor dispatches them to domain event handlers after commit.
- **Integration events**: a handler maps cart events to flat integration events and publishes them to a RabbitMQ topic exchange with publisher confirms.
- **Optimistic concurrency**: two requests that change the same cart at the same time cannot both win; the second gets `409 Conflict`.

## Architecture

Project references:

```mermaid
flowchart TB
    API["ShoppingCart.API<br/>Minimal API endpoints · ProblemDetails · health checks · OpenAPI"]

    subgraph Core["ShoppingCart.Core"]
        D["ShoppingCart.Domain<br/>Cart aggregate · entities · value objects · domain events"]
        A["ShoppingCart.Application<br/>Commands & queries (MediatR) · DTOs · integration events · ports"]
    end

    subgraph Infra["ShoppingCart.Infrastructure"]
        P["ShoppingCart.Persistence<br/>EF Core DbContext · repositories · UnitOfWork · event store · outbox"]
        I["ShoppingCart.Infrastructure<br/>RabbitMQ message bus"]
    end

    API --> A
    API --> P
    API --> I
    A --> D
    P --> A
    I --> A

    P -.-> PG[(PostgreSQL)]
    I -.-> MQ[(RabbitMQ)]
```

Dependencies point inward. The Domain has no dependencies. The Application layer defines the ports (`ICartWriteRepository`, `ICartReadRepository`, `IUnitOfWork`, `IEventStore`, `IMessageBus`), and the outer projects implement them. The API project is the composition root.

### What happens on a write

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant E as Endpoint
    participant H as Command handler
    participant Cart as Cart aggregate
    participant U as UnitOfWork / DbContext
    participant DB as PostgreSQL
    participant O as OutboxProcessor
    participant P as CartIntegrationEventPublisher
    participant MQ as RabbitMQ

    C->>E: POST /api/carts/{id}/items
    E->>H: AddCartItemCommand (MediatR)
    H->>U: load cart with items
    H->>Cart: AddItem(variant, merchant, quantity)
    Cart-->>Cart: raise CartItemAddedEvent, Version + 1
    H->>U: SaveChangesAsync()
    Note over U: SaveChanges interceptor:<br/>audit columns, StoredEvents rows,<br/>OutboxMessages rows
    U->>DB: one transaction: cart + items + events + outbox<br/>(UPDATE ... WHERE Version = loaded version)
    U-->>O: signal
    H-->>E: Result<CartDto>
    E-->>C: 200 OK + cart
    O->>DB: SELECT ... FOR UPDATE SKIP LOCKED
    O->>P: dispatch domain event
    P->>MQ: publish CartItemAddedIntegrationEvent (confirmed)
    O->>DB: mark outbox message processed
```

The client gets its response as soon as the transaction commits. Publishing happens afterwards and is retried by the outbox processor if RabbitMQ is unavailable, so a committed change is never lost and an event is never published for a change that rolled back.

### Domain model

```mermaid
classDiagram
    class Cart {
        CartId Id
        CartGuid CartGuid
        long Version
        IReadOnlyList~CartItem~ Items
        decimal TotalPrice
        +Create(CartGuid) Cart
        +AddItem(Variant, Merchant, int) CartItem
        +UpdateItemQuantity(CartItemId, int)
        +RemoveItem(CartItemId)
        +Clear()
    }
    class CartItem {
        CartItemId Id
        int Quantity
        +GetTotalPrice() decimal
    }
    class Variant {
        VariantId Id
        decimal Price
    }
    class Merchant {
        MerchantId Id
        string Name
    }
    Cart "1" *-- "many" CartItem
    CartItem --> Variant : snapshot
    CartItem --> Merchant : snapshot
```

Domain events: `CartCreatedEvent`, `CartItemAddedEvent`, `CartItemUpdatedEvent`, `CartItemRemovedEvent`, `CartClearedEvent`. Each derives from the `DomainEvent` record, whose `EventId` (a version 7 GUID) and `OccurredOn` (UTC) are set once when the event is raised.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 (LTS), C# with nullable reference types, warnings as errors |
| HTTP | ASP.NET Core minimal APIs, ProblemDetails, `Microsoft.AspNetCore.OpenApi` |
| CQRS / mediator | MediatR 12.5 (the last Apache 2.0 release) |
| ORM / database | EF Core 10, Npgsql 10, PostgreSQL 17 |
| Event store | PostgreSQL table `StoredEvents` (append-only, written by EF Core) |
| Messaging | RabbitMQ 4 via `RabbitMQ.Client` 7 (publisher confirms) |
| Tests | xUnit v3, `WebApplicationFactory`, Testcontainers (PostgreSQL, RabbitMQ) |
| Packaging | Central package management (`Directory.Packages.props`), `.slnx` solution, Docker |

## Project structure

```text
ShoppingCartProject.slnx
Directory.Build.props                 # net10.0, nullable, warnings as errors, NuGet audit
Directory.Packages.props              # central package versions
Dockerfile · docker-compose.yml
src/services/ShoppingCartService/
├── ShoppingCart.API/
│   ├── Endpoints/                    # CartEndpoints, Result<T> -> HTTP mapping
│   ├── ErrorHandling/                # GlobalExceptionHandler (ProblemDetails)
│   ├── Program.cs                    # composition root, health checks, OpenAPI
│   └── ShoppingCart.API.http         # sample requests
├── ShoppingCart.Core/
│   ├── ShoppingCart.Domain/
│   │   ├── Common/                   # Entity<T>, Aggregate<TId>, DomainEvent, IStronglyTypedId, repository contracts
│   │   ├── Exceptions/               # DomainException, NotFoundException
│   │   ├── Validations/              # DomainValidation guard clauses
│   │   └── ShoppingCart/
│   │       ├── Aggregates/           # Cart
│   │       ├── Entities/             # CartItem, Variant, Merchant
│   │       ├── Events/               # CartCreated / ItemAdded / ItemUpdated / ItemRemoved / Cleared
│   │       ├── Exceptions/           # CartException
│   │       └── ValueObjects/         # CartId, CartGuid, CartItemId, VariantId, MerchantId
│   └── ShoppingCart.Application/
│       ├── Cart/Commands/            # Create, AddItem, UpdateItemQuantity, RemoveItem, Clear
│       ├── Cart/Queries/             # GetCart, GetCartEvents
│       ├── Cart/Events/              # integration events + CartIntegrationEventPublisher
│       ├── Cart/Dtos/                # CartDto, CartItemDto, CartEventDto
│       ├── Cart/Repositories/        # ICartWriteRepository, ICartReadRepository
│       └── Common/                   # ports, Result<T>, serialization of strongly typed ids
└── ShoppingCart.Infrastructure/
    ├── ShoppingCart.Persistence/
    │   ├── Cart/Repositories/        # CartWriteRepository, CartReadRepository
    │   ├── Common/Data/              # DbContext, SaveChanges interceptor, UnitOfWork, EventStore, Migrations/
    │   ├── Common/Events/            # DomainEventDispatcher
    │   ├── Common/Outbox/            # OutboxMessage, OutboxProcessor, OutboxSignal
    │   └── Common/Serialization/     # EventSerializer
    └── ShoppingCart.Infrastructure/
        └── Messaging/                # RabbitMqMessageBus, options, health check
tests/
├── ShoppingCart.UnitTests/           # domain, serializer, integration event mapping
└── ShoppingCart.IntegrationTests/    # API + PostgreSQL + RabbitMQ in containers
```

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (for the compose stack and the integration tests)

### Run everything with Docker Compose

```bash
git clone https://github.com/gokhanertogan/ShoppingCartProject.git
cd ShoppingCartProject
docker compose up --build
```

This starts PostgreSQL 17, RabbitMQ 4 (management UI on `http://localhost:15672`, user `shoppingcart` / `Your_password123`) and the API on `http://localhost:8080`. The API applies the EF Core migrations at startup. If a port is taken, override it, for example:

```bash
POSTGRES_PORT=55432 RABBITMQ_PORT=55672 RABBITMQ_MANAGEMENT_PORT=55673 API_PORT=58080 docker compose up --build
```

`docker compose down -v` stops the stack and deletes its volumes.

### Run the API from source

Start only the infrastructure, then run the API (the `http` launch profile listens on `http://localhost:5080` and applies migrations in Development):

```bash
docker compose up -d postgres rabbitmq
dotnet run --project src/services/ShoppingCartService/ShoppingCart.API --launch-profile http
```

The OpenAPI document is at `/openapi/v1.json` in Development (or when `OpenApi__Enabled=true`).

### Build and test

```bash
dotnet build ShoppingCartProject.slnx
dotnet test ShoppingCartProject.slnx    # integration tests need Docker running
```

### Migrations

Migrations live in `ShoppingCart.Persistence/Common/Data/Migrations`. `dotnet-ef` is pinned in a local tool manifest:

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  -p src/services/ShoppingCartService/ShoppingCart.Infrastructure/ShoppingCart.Persistence \
  -o Common/Data/Migrations
dotnet ef migrations script --idempotent \
  -p src/services/ShoppingCartService/ShoppingCart.Infrastructure/ShoppingCart.Persistence
```

Migrating at startup (`Database:MigrateOnStartup`) is convenient for local runs and the compose stack. In production, apply the idempotent script or a migration bundle as a deployment step instead.

## API

All routes are under `/api/carts`. Successful calls return the cart (`CartDto`); errors return `application/problem+json`.

| Method | Route | Success | Errors |
|---|---|---|---|
| POST | `/api/carts` | `201 Created` + `Location`, cart | `400` invalid input |
| GET | `/api/carts/{cartId}` | `200`, cart | `404` |
| GET | `/api/carts/{cartId}/events` | `200`, event history from the event store | `404` |
| POST | `/api/carts/{cartId}/items` | `200`, cart | `400`, `404`, `409` |
| PUT | `/api/carts/{cartId}/items/{itemId}` | `200`, cart | `400`, `404`, `409` |
| DELETE | `/api/carts/{cartId}/items/{itemId}` | `200`, cart | `404`, `409` |
| DELETE | `/api/carts/{cartId}/items` | `200`, cart (cleared) | `404`, `409` |
| GET | `/health/live` | `200` when the process is up | |
| GET | `/health/ready` | `200` when PostgreSQL and RabbitMQ are reachable | `503` |

Rules enforced by the domain (violations return `400`): quantities must be between 1 and 1000 per line, prices cannot be negative, merchant names are required (max 200 characters), and IDs cannot be empty GUIDs.

### Sample requests

Create a cart. The two lines have the same variant and merchant, so they are merged into one line with quantity 3:

```bash
curl -i -X POST http://localhost:8080/api/carts -H 'Content-Type: application/json' -d '{
  "cartGuid": "0aa14797-0cea-4c36-8a89-0a0d32440f98",
  "items": [
    { "variant": { "id": "fab3dc15-0435-4536-8f18-99674a585b04", "price": 10.50 },
      "merchant": { "id": "978ee145-e91d-4453-af47-f87d57de715e", "name": "Acme" }, "quantity": 2 },
    { "variant": { "id": "fab3dc15-0435-4536-8f18-99674a585b04", "price": 10.50 },
      "merchant": { "id": "978ee145-e91d-4453-af47-f87d57de715e", "name": "Acme" }, "quantity": 1 }
  ]
}'
```

```http
HTTP/1.1 201 Created
Location: /api/carts/01a1679d-8daf-73bc-b492-536e992bf77e

{"id":"01a1679d-8daf-73bc-b492-536e992bf77e","cartGuid":"0aa14797-0cea-4c36-8a89-0a0d32440f98","version":3,
 "items":[{"id":"01a1679d-8db1-7fc7-973c-7c62daba1d1f","variantId":"fab3dc15-0435-4536-8f18-99674a585b04",
           "unitPrice":10.5,"merchantId":"978ee145-e91d-4453-af47-f87d57de715e","merchantName":"Acme",
           "quantity":3,"totalPrice":31.5}],
 "totalQuantity":3,"totalPrice":31.5,"createdAt":"2026-10-23T07:15:41.1042012Z","lastModified":null}
```

Add an item, change its quantity, remove it, and clear the cart:

```bash
CART=01a1679d-8daf-73bc-b492-536e992bf77e
curl -X POST http://localhost:8080/api/carts/$CART/items -H 'Content-Type: application/json' -d '{
  "variant": { "id": "dd86be80-e2ac-4966-a7df-cd63120a585d", "price": 4.25 },
  "merchant": { "id": "978ee145-e91d-4453-af47-f87d57de715e", "name": "Acme" }, "quantity": 2 }'
curl -X PUT http://localhost:8080/api/carts/$CART/items/<itemId> -H 'Content-Type: application/json' -d '{ "quantity": 5 }'
curl -X DELETE http://localhost:8080/api/carts/$CART/items/<itemId>
curl -X DELETE http://localhost:8080/api/carts/$CART/items
```

Read the cart's history from the event store:

```bash
curl http://localhost:8080/api/carts/$CART/events
```

```json
[
  {"version":1,"eventType":"CartCreatedEvent","eventId":"01a1679d-8db0-719f-88cb-dadaa3fa281b","occurredOn":"...","recordedAt":"...","data":{"cartId":"01a1679d-...","cartGuid":"0aa14797-...","eventId":"01a1679d-8db0-...","occurredOn":"..."}},
  {"version":2,"eventType":"CartItemAddedEvent", ...},
  {"version":3,"eventType":"CartItemUpdatedEvent", ...},
  {"version":4,"eventType":"CartItemAddedEvent", ...},
  {"version":5,"eventType":"CartItemUpdatedEvent", ...},
  {"version":6,"eventType":"CartItemRemovedEvent", ...},
  {"version":7,"eventType":"CartClearedEvent", ...}
]
```

A rule violation:

```bash
curl -X PUT http://localhost:8080/api/carts/$CART/items/<itemId> -H 'Content-Type: application/json' -d '{ "quantity": 0 }'
```

```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,
 "detail":"quantity should be greater than zero","errors":["quantity should be greater than zero"],"traceId":"0HNPGQLKIKD6O:00000001"}
```

`src/services/ShoppingCartService/ShoppingCart.API/ShoppingCart.API.http` has the same requests for the VS Code REST Client, Rider and Visual Studio.

## Integration events

Integration events are published to the durable topic exchange `shopping-cart.events` as persistent JSON messages. The API declares the exchange at startup, so consumers can bind to it before the first event is published. The routing key and the AMQP `type` are the event name; the AMQP `message_id` is the event id, which is the id of the domain event it came from, so it stays the same across redeliveries and consumers can de-duplicate on it.

| Domain event | Integration event (routing key) | Payload |
|---|---|---|
| `CartCreatedEvent` | `CartCreatedIntegrationEvent` | `eventId, occurredOn, cartId, cartGuid` |
| `CartItemAddedEvent` | `CartItemAddedIntegrationEvent` | `… cartItemId, variantId, merchantId, price, quantity` |
| `CartItemUpdatedEvent` | `CartItemQuantityChangedIntegrationEvent` | `… cartItemId, price, quantity` |
| `CartItemRemovedEvent` | `CartItemRemovedIntegrationEvent` | `… cartItemId, variantId, merchantId` |
| `CartClearedEvent` | `CartClearedIntegrationEvent` | `… cartId` |

A consumer binds its own queue to the exchange, with `#` for everything or with specific routing keys. `IMessageBus.SubscribeAsync<T>` does this for in-process consumers: it declares a durable queue `shopping-cart.<EventName>`, acknowledges a message after the handler succeeds and rejects it (without requeue) if the handler throws.

## Configuration

| Key | Default | Purpose |
|---|---|---|
| `ConnectionStrings:ShoppingCartDb` | `Host=localhost;Port=5432;Database=shoppingcart;...` | PostgreSQL |
| `ConnectionStrings:RabbitMq` | `amqp://shoppingcart:Your_password123@localhost:5672/` | RabbitMQ |
| `RabbitMq:ExchangeName` | `shopping-cart.events` | Exchange for integration events |
| `RabbitMq:QueuePrefix` | `shopping-cart` | Prefix of queues created by `SubscribeAsync` |
| `Outbox:PollingInterval` | `00:00:02` | Poll interval when no commit has signalled the processor |
| `Outbox:BatchSize` | `50` | Messages claimed per batch |
| `Outbox:MaxAttempts` | `10` | After this many failures a message is left for manual handling |
| `Database:MigrateOnStartup` | `false` (`true` in Development and in compose) | Apply EF Core migrations at startup |
| `OpenApi:Enabled` | `false` (always on in Development) | Serve `/openapi/v1.json` |

The credentials in `appsettings.json` and `docker-compose.yml` are development placeholders. Use user secrets or environment variables for anything else.

## Design notes and trade-offs

- **Strongly typed IDs.** `CartId`, `CartItemId`, `VariantId`, `MerchantId` and `CartGuid` wrap `Guid`, reject `Guid.Empty` and implement `IStronglyTypedId`. EF Core maps them with value converters, and a JSON converter writes them as plain GUIDs.
- **Cart lines snapshot the variant and merchant.** `Variant` (price) and `Merchant` (name) are owned types of `CartItem`, so each line keeps the price and merchant name from the moment it was added, independent of the catalog.
- **Behavior lives in the aggregate.** Quantity rules, the merge of duplicate lines and item lookup are enforced by `Cart`. Rule violations raise `DomainException` (or `NotFoundException`), which the handlers turn into `400` (or `404`) results.
- **Versioned aggregates.** Every domain event increments `Cart.Version`. The version is the EF Core concurrency token of the cart row and the version of its event stream, so a stale write fails on the `UPDATE ... WHERE "Version" = @loaded` and on the unique `(StreamId, Version)` index. Both surface as `409 Conflict`.
- **Event store in PostgreSQL instead of EventStoreDB.** The earlier draft used the EventStoreDB TCP client (`EventStore.Client` 22), which is deprecated, and wrote to a second database outside the EF Core transaction. A table in the same database keeps the cart state and its history consistent with a single local transaction, and needs no extra infrastructure. The trade-off: no server-side projections or subscriptions; the outbox covers the "notify others" part. Events are serialized by their concrete type and resolved back by type name.
- **Outbox instead of in-process dispatch.** Dispatching domain events right after `SaveChanges` loses them if the process stops between the commit and the publish. The outbox row is committed with the change, and the processor retries until the handler succeeds. Delivery is therefore at least once and in order (by `OccurredOn`, then stream version); handlers and consumers must be idempotent, and the stable event id makes that possible. Rows are claimed with `FOR UPDATE SKIP LOCKED`, so several instances can run the processor.
- **Separate read and write repositories.** The write repository loads tracked aggregates; the read repository uses no-tracking queries. Handlers commit through `IUnitOfWork`, not through repositories.
- **MediatR is pinned to 12.x**, the last version under the Apache 2.0 license.

## Known limitations

- There is no authentication or authorization; `CartGuid` is stored and indexed but not yet used to look carts up (for example by session or user).
- Variant prices and merchant names come from the client request. A real deployment would read them from catalog and merchant services.
- Processed outbox messages are kept; there is no cleanup job yet.
- Events are not used to rebuild the aggregate (the cart state is stored as rows); the event log is an audit trail and the source of the outbox.
- Concurrent updates are rejected with `409`, not retried automatically.

## License

[MIT](LICENSE) © 2025 Gökhan Ertoğan
