// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Application.Common.Access;
using Microsoft.AspNetCore.Authentication;

namespace Adwais.Api.Services;

public sealed class CurrentAccessService(IHttpContextAccessor httpContextAccessor) : ICurrentAccess
{
    public AccessScope? Scope => Resolve(httpContextAccessor.HttpContext?.User);

    public static AccessScope? Resolve(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        var organizationId = TryParseGuid(principal.FindFirstValue(AccessClaimTypes.OrganizationId));
        var tenantId = TryParseGuid(principal.FindFirstValue(AccessClaimTypes.TenantId));

        if (organizationId is null)
        {
            return principal.IsInRole("Admin") ? new AccessScope(null, null) : null;
        }

        return new AccessScope(organizationId, tenantId);
    }

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;
}
