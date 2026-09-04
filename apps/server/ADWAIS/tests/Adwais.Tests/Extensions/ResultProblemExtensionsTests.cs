using Adwais.Api.Extensions;
using Adwais.Application.Common.Errors;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Tests.Extensions;

public class ResultProblemExtensionsTests
{
    [Theory]
    [InlineData("not-found", 404)]
    [InlineData("configuration", 409)]
    [InlineData("conflict", 409)]
    [InlineData("scope", 403)]
    [InlineData("provider", 502)]
    [InlineData("timeout", 504)]
    public void ToProblem_MapsKnownErrorsToStableProblemDetails(string kind, int expectedStatus)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-123" };
        context.Request.Path = "/api/test";

        var action = Result.Fail(CreateError(kind)).ToProblem(context);

        var objectResult = Assert.IsType<ObjectResult>(action);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("/api/test", problem.Instance);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.StartsWith("https://adwais.app/problems/", problem.Type);
    }

    [Fact]
    public void ToProblem_MapsValidationErrorsToValidationProblemDetails()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-123" };
        context.Request.Path = "/api/test";
        var errors = new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required.", "Email is invalid."]
        };

        var action = Result.Fail(new ValidationError(errors)).ToProblem(context);

        var objectResult = Assert.IsType<BadRequestObjectResult>(action);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(errors["email"], problem.Errors["email"]);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
    }

    [Fact]
    public void ToProblem_DoesNotExposeUnknownErrors()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-123" };

        var action = Result.Fail("database password: secret").ToProblem(context);

        var objectResult = Assert.IsType<ObjectResult>(action);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.DoesNotContain("secret", problem.Detail);
    }

    private static ApplicationError CreateError(string kind) => kind switch
    {
        "not-found" => new NotFoundError("User", "abc"),
        "configuration" => new ConfigurationError("Provider settings are incomplete."),
        "conflict" => new ConflictError("The user already belongs to this organization."),
        "scope" => new ScopeDeniedError("organization", "platform"),
        "provider" => new ProviderError("The provider returned an invalid response."),
        "timeout" => new ProviderTimeoutError("The provider timed out."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
