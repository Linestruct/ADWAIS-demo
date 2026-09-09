using Adwais.Application.Common.Errors;

namespace Adwais.Tests.Errors;

public class ApplicationErrorsTests
{
    [Fact]
    public void ValidationError_PreservesAllFieldMessages()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required.", "Email is invalid."]
        };

        var result = new ValidationError(errors);

        Assert.Equal("validation", result.Code);
        Assert.Equal("Validation failed", result.Title);
        Assert.Equal(errors, result.ErrorsByField);
    }

    [Fact]
    public void NotFoundError_ExposesAStableCodeAndSafeDetail()
    {
        var result = new NotFoundError("User", "abc");

        Assert.Equal("not-found", result.Code);
        Assert.Equal("Not found", result.Title);
        Assert.Equal("User 'abc' was not found.", result.Detail);
    }
}
