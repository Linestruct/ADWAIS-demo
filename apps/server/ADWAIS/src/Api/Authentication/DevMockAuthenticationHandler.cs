// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adwais.Api.Authentication;

/// <summary>
/// Development only. Authenticates as the platform admin when no
/// Authorization header is present. Uses the same claim builder as the
/// production claims transformation.
/// </summary>
public class DevMockAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IWebHostEnvironment environment,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IWebHostEnvironment _environment = environment;
    private readonly IConfiguration _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var mockOrganizationId = ParseOrganizationId(_configuration["DEV_MOCK_ORG_ID"]);
        var principal = BuildForDev(_environment.IsDevelopment(), Request.Headers.ContainsKey("Authorization"), mockOrganizationId);
        if (principal is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    public static ClaimsPrincipal? BuildForDev(bool isDevelopment, bool hasAuthHeader, Guid? mockOrganizationId)
    {
        if (!isDevelopment || hasAuthHeader)
        {
            return null;
        }

        var scope = mockOrganizationId is { } orgId
            ? new AccessScope(orgId, null, [UserRole.Admin])
            : new AccessScope(null, null, [UserRole.PlatformAdmin]);
        var identity = AccessClaimsBuilder.Build(
            AnalyticsDbContext.SystemUserGuid,
            scope);
        return new ClaimsPrincipal(identity);
    }

    private static Guid? ParseOrganizationId(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;
}
