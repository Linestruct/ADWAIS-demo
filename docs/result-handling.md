# Result handling

Use FluentResults at an application-service boundary when the caller must handle an expected outcome. A result makes the outcome explicit. An exception interrupts execution.

## Choose the return type

| Situation | Return type |
|---|---|
| A caller must distinguish success, absence, denial, validation, or conflict | `Task<Result<T>>` |
| A command has no success value but can fail in an expected way | `Task<Result>` |
| Absence is normal and needs no reason | `Task<T?>` |
| An empty collection is normal | `Task<IReadOnlyList<T>>` |
| A bug, broken invariant, outage, cancellation, or retryable job failure | Throw |

Do not use `Result` for every method. Keep calculations, query helpers, and private implementation details simple unless their caller has a real failure decision to make.

Application services return domain objects or application DTOs. They do not return API response DTOs.

```csharp
Task<Result<User>> UpdateUserAsync(...);
Task<Result<UptimeMonitor>> CreateMonitorAsync(...);
Task<Result<KpiDto>> GetKpisAsync(...);
```

The API controller maps a successful value to its response DTO.

## Expected errors

Use the typed errors in `Application/Common/Errors`.

| Error | HTTP status | Use when |
|---|---:|---|
| `NotFoundError` | 404 | The requested resource does not exist. |
| `ValidationError` | 400 | The request has one or more invalid fields. |
| `ScopeDeniedError` | 403 | The authenticated caller cannot access the resource or scope. |
| `ConflictError` | 409 | A valid request conflicts with current state. |
| `ConfigurationError` | 409 | Configuration blocks an otherwise valid operation. |
| `ProviderError` | 502 | An upstream provider failed or returned an invalid response. |
| `ProviderTimeoutError` | 504 | An upstream provider timed out. |

Return one primary non-validation error. Return one `ValidationError` that contains all field errors. Do not combine unrelated errors and rely on the API mapper to choose one.

```csharp
if (tenant is null)
    return Result.Fail<Tenant>(new NotFoundError("tenant", tenantId));

if (!visibleTenantIds.Contains(tenantId))
    return Result.Fail<Tenant>(new ScopeDeniedError(
        "the requested tenant", "the current scope"));
```

## Controller pattern

Controllers own successful HTTP responses. The result mapper owns failed responses.

```csharp
var result = await service.UpdateUserAsync(id, request.Name, request.Role, ct);
if (result.IsFailed)
    return result.ToProblem(HttpContext);

return Ok(Map(result.Value));
```

Keep each endpoint's success contract. For example, a create action can return `CreatedAtAction`, a delete can return `NoContent`, and an enqueue action can return `Accepted`.

Add response metadata for every failure status that an endpoint can return. Regenerate OpenAPI and the web client after changing that contract.

## Downstream exceptions

`Result<T>` does not convert exceptions automatically. Translate a downstream exception only when it represents an expected outcome at this boundary.

```csharp
try
{
    return Result.Ok(await provider.GetWeatherAsync(ct));
}
catch (TaskCanceledException) when (!ct.IsCancellationRequested)
{
    return Result.Fail<WeatherDto>(new ProviderTimeoutError(
        "The weather provider timed out."));
}
```

Do not catch `Exception` to return a failed result. Let unexpected database failures, programming errors, invariant failures, cancellation, and retryable job failures throw.

## Exceptions and legacy contracts

The global exception handler maps generic unhandled exceptions to 500. Do not throw `ArgumentException`, `KeyNotFoundException`, `UnauthorizedAccessException`, or `ConfigurationException` to produce a client response.

Calendar-feed and webhook compatibility paths are exceptions to this rule. They use `HttpContractException` for their deliberate, documented 4xx behavior. Do not use `HttpContractException` in new application code.

Background jobs must record their failure state and then throw so Hangfire can retry them.

## Observability

A failed result is an expected application outcome. Do not automatically write it as a `SystemEvent` error. Record request metrics or structured logs by endpoint, status, and typed error code. Add a security audit only when the failure itself is significant.

Unhandled exceptions and job failures are operational errors. They can create `SystemEvent` records and error logs with the trace information needed to investigate them.

## Tests

For a result-based operation, test both layers:

1. The service returns the expected typed failure without throwing.
2. The controller maps that failure to the intended ProblemDetails status and preserves its success response.

When API metadata changes, run the server tests, regenerate OpenAPI and the web client, then run the web tests and production build.
