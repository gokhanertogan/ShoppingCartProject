using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ShoppingCart.API.Endpoints;
using ShoppingCart.API.ErrorHandling;
using ShoppingCart.Application;
using ShoppingCart.Application.Common.Serialization;
using ShoppingCart.Infrastructure;
using ShoppingCart.Infrastructure.Messaging;
using ShoppingCart.Persistence;
using ShoppingCart.Persistence.Common.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory()));

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ShoppingCartDbContext>("postgres", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
}

// Liveness: the process is up. Readiness: PostgreSQL and RabbitMQ are reachable.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapCartEndpoints();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

await app.RunAsync();

public partial class Program;
