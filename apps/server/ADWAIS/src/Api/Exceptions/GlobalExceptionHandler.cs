// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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

        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
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
            ArgumentException or InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1"
            ),
            HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.NotFound => (
                StatusCodes.Status404NotFound,
                "Downstream Resource Not Found",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
            )
        };
    }
}

