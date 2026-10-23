using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.Application.Common.Persistence;
using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.API.ErrorHandling;

/// <summary>Turns unhandled exceptions into RFC 9457 ProblemDetails responses.</summary>
public sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request", badRequest.Message),
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Not found", notFound.Message),
            DomainException domain => (StatusCodes.Status400BadRequest, "Bad request", domain.Message),
            // Another request changed the same cart first (Version concurrency token or event stream version).
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Conflict", "The cart was modified by another request. Reload it and retry."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title, Detail = detail },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);
}
