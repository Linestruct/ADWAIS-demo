// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Exceptions;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Exceptions;

/// <summary>
/// Intercepts all unhandled exceptions to provide consistent ProblemDetails responses 
/// and persist audit logs to the database.
/// </summary>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IServiceProvider serviceProvider) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<ISystemEventService>();

        var (statusCode, title, type) = MapException(exception);

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected: {Message}", exception.Message);
        }

        await eventService.LogErrorAsync("GlobalExceptionHandler", exception.Message, exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Instance = httpContext.Request.Path,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Type) MapException(Exception exception)
    {
        return exception switch
        {
            HttpContractException contract => (
                contract.StatusCode,
                contract.Title,
                "about:blank"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
            )
        };
    }
}

