// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
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

        var roles = principal.FindAll(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
            .Select(claim => claim.Value)
            .Where(value => Enum.TryParse<UserRole>(value, out _))
            .Select(Enum.Parse<UserRole>)
            .Distinct()
            .ToList();

        if (principal.HasClaim(AccessClaimTypes.IsPlatformAdmin, "true"))
        {
            return new AccessScope(null, null, roles);
        }

        var organizationId = TryParseGuid(principal.FindFirstValue(AccessClaimTypes.OrganizationId));
        if (organizationId is null)
        {
            return null;
        }

        var tenantId = TryParseGuid(principal.FindFirstValue(AccessClaimTypes.TenantId));
        return new AccessScope(organizationId, tenantId, roles);
    }

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;
}
