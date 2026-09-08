using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NexaECommerce.Server.Platform.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = Map(exception);

        httpContext.Response.StatusCode = error.Status;

        var problem = new ProblemDetails
        {
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
            Type =
                $"https://docs.nexaecommerce.dev/errors/" +
                $"{error.Code.ToLowerInvariant().Replace('_', '-')}",
            Instance = httpContext.Request.Path,
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] =
            Activity.Current?.Id ??
            httpContext.TraceIdentifier;

        if (error.Errors is not null)
        {
            problem.Extensions["errors"] = error.Errors;
        }

        if (exception is DbUpdateConcurrencyException concurrencyException)
        {
            var diagnostics = concurrencyException.Entries
                .Select(DescribeConcurrencyEntry)
                .ToArray();

            logger.LogError(
                concurrencyException,
                "EF concurrency conflict during {Method} {Path}. " +
                "Affected entities: {@Entries}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                diagnostics);

            if (httpContext.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .IsEnvironment("Testing"))
            {
                problem.Extensions["concurrency"] = diagnostics;
            }
        }
        else if (error.Status >= 500)
        {
            logger.LogError(
                exception,
                "Unhandled exception");
        }
        else
        {
            logger.LogWarning(
                "Handled {Code}: {Message}",
                error.Code,
                exception.Message);
        }

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problem,
            });
    }

    private static object DescribeConcurrencyEntry(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        return new
        {
            entity =
                entry.Metadata.ClrType.FullName ??
                entry.Metadata.Name,

            state = entry.State.ToString(),

            keys =
                primaryKey?.Properties
                    .Select(property => property.Name)
                    .ToArray()
                ?? Array.Empty<string>(),

            concurrencyTokens =
                entry.Metadata.GetProperties()
                    .Where(property =>
                        property.IsConcurrencyToken)
                    .Select(property =>
                        property.Name)
                    .ToArray(),
        };
    }

    private static ErrorInfo Map(
        Exception exception)
    {
        return exception switch
        {
            DomainException domainException =>
                new(
                    domainException.Status,
                    domainException.Code,
                    ReasonPhrase(domainException.Status),
                    domainException.Message,
                    domainException.Errors),

            KeyNotFoundException =>
                new(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Not Found",
                    exception.Message,
                    null),

            ArgumentException =>
                new(
                    StatusCodes.Status400BadRequest,
                    "BAD_REQUEST",
                    "Bad Request",
                    exception.Message,
                    null),

            InvalidOperationException =>
                new(
                    StatusCodes.Status409Conflict,
                    "CONFLICT",
                    "Conflict",
                    exception.Message,
                    null),

            DbUpdateConcurrencyException =>
                new(
                    StatusCodes.Status409Conflict,
                    "CONCURRENCY_CONFLICT",
                    "Conflict",
                    "This record was changed by someone else. Refresh and try again.",
                    null),

            _ =>
                new(
                    StatusCodes.Status500InternalServerError,
                    "INTERNAL_ERROR",
                    "Internal Server Error",
                    "An unexpected error occurred.",
                    null),
        };
    }

    private static string ReasonPhrase(int status)
    {
        return status switch
        {
            StatusCodes.Status400BadRequest =>
                "Bad Request",

            StatusCodes.Status403Forbidden =>
                "Forbidden",

            StatusCodes.Status404NotFound =>
                "Not Found",

            StatusCodes.Status409Conflict =>
                "Conflict",

            _ =>
                "Error",
        };
    }

    private readonly record struct ErrorInfo(
        int Status,
        string Code,
        string Title,
        string Detail,
        IReadOnlyDictionary<string, string[]>? Errors);
}