using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace ShoppingCart.IntegrationTests;

/// <summary>
/// Hosts the real API in memory against PostgreSQL and RabbitMQ containers. The schema is created by
/// the EF Core migrations at startup, and the outbox processor and RabbitMQ bus run as in production.
/// </summary>
public sealed class ShoppingCartApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine").Build();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ShoppingCartDb", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:RabbitMq", _rabbitMq.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.250");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ShoppingCartApiFactory>
{
    public const string Name = "api";
}
