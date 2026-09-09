// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Exceptions;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Adwais.Api.Exceptions;

/// <summary>
/// Intercepts unhandled exceptions to provide consistent ProblemDetails responses
/// and persist a best-effort operational incident.
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
        var (statusCode, title, type) = MapException(exception);

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected: {Message}", exception.Message);
        }

        if (statusCode >= 500)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var eventService = scope.ServiceProvider.GetRequiredService<ISystemEventService>();
                await eventService.LogErrorAsync(
                    "GlobalExceptionHandler",
                    "Unhandled exception while processing a request.",
                    exception);
            }
            catch (Exception loggingException)
            {
                // The response for the original failure must not depend on
                // diagnostics storage or its own dependency graph.
                logger.LogError(loggingException, "Could not persist the global exception event.");
            }
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Instance = httpContext.Request.Path,
            Detail = statusCode >= 500
                ? "The server could not complete the request. Use the request id when contacting support."
                : exception.Message
        };
        problemDetails.Extensions["requestId"] = httpContext.TraceIdentifier;
        var traceId = Activity.Current?.TraceId.ToHexString();
        if (traceId is not null)
            problemDetails.Extensions["traceId"] = traceId;

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

