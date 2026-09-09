using Adwais.Application.Common.Errors;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Adwais.Api.Extensions;

public static class ResultProblemExtensions
{
    public static ActionResult ToProblem(this Result result, HttpContext httpContext)
        => ToProblem(result.Errors, httpContext);

    public static ActionResult ToProblem<T>(this Result<T> result, HttpContext httpContext)
        => ToProblem(result.Errors, httpContext);

    private static ActionResult ToProblem(IReadOnlyList<IError> errors, HttpContext httpContext)
    {
        if (errors.Count == 1 && errors[0] is ValidationError validationError)
        {
            var validationProblem = new ValidationProblemDetails(
                validationError.ErrorsByField.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = validationError.Title,
                Type = ProblemType(validationError),
                Detail = validationError.Detail,
                Instance = httpContext.Request.Path
            };
            AddCorrelation(validationProblem, httpContext);
            return new BadRequestObjectResult(validationProblem);
        }

        if (errors.Count == 1 && errors[0] is ApplicationError applicationError)
        {
            var problem = new ProblemDetails
            {
                Status = StatusFor(applicationError),
                Title = applicationError.Title,
                Type = ProblemType(applicationError),
                Detail = applicationError.Detail,
                Instance = httpContext.Request.Path
            };
            AddCorrelation(problem, httpContext);
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }

        var unexpectedProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Type = "https://adwais.app/problems/internal-error",
            Detail = "The operation failed unexpectedly.",
            Instance = httpContext.Request.Path
        };
        AddCorrelation(unexpectedProblem, httpContext);
        return new ObjectResult(unexpectedProblem) { StatusCode = unexpectedProblem.Status };
    }

    private static void AddCorrelation(ProblemDetails problem, HttpContext httpContext)
    {
        problem.Extensions["requestId"] = httpContext.TraceIdentifier;
        var traceId = Activity.Current?.TraceId.ToHexString();
        if (traceId is not null)
            problem.Extensions["traceId"] = traceId;
    }

    private static int StatusFor(ApplicationError error) => error switch
    {
        NotFoundError => StatusCodes.Status404NotFound,
        ValidationError => StatusCodes.Status400BadRequest,
        ConfigurationError or ConflictError => StatusCodes.Status409Conflict,
        ScopeDeniedError => StatusCodes.Status403Forbidden,
        ProviderError => StatusCodes.Status502BadGateway,
        ProviderTimeoutError => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ProblemType(ApplicationError error) => $"https://adwais.app/problems/{error.Code}";
}
