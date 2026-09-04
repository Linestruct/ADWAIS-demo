using FluentResults;

namespace Adwais.Application.Common.Errors;

public abstract class ApplicationError(string code, string title, string detail) : Error(detail)
{
    public string Code { get; } = code;
    public string Title { get; } = title;
    public string Detail { get; } = detail;
}

public sealed class NotFoundError(string resource, object? id = null)
    : ApplicationError(
        "not-found",
        "Not found",
        id is null ? $"{resource} was not found." : $"{resource} '{id}' was not found.")
{
    public string Resource { get; } = resource;
    public object? Id { get; } = id;
}

public sealed class ValidationError(IReadOnlyDictionary<string, string[]> errorsByField)
    : ApplicationError("validation", "Validation failed", "One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> ErrorsByField { get; } = errorsByField;
}

public sealed class ConfigurationError(string detail)
    : ApplicationError("configuration-conflict", "Configuration conflict", detail);

public sealed class ConflictError(string detail)
    : ApplicationError("conflict", "Conflict", detail);

public sealed class ScopeDeniedError(string required, string actual)
    : ApplicationError("scope-denied", "Forbidden", "The current scope cannot perform this operation.")
{
    public string Required { get; } = required;
    public string Actual { get; } = actual;
}

public sealed class ProviderError(string detail)
    : ApplicationError("provider-failure", "Upstream provider failed", detail);

public sealed class ProviderTimeoutError(string detail)
    : ApplicationError("provider-timeout", "Upstream provider timed out", detail);
