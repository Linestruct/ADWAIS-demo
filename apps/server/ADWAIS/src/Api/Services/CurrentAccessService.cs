// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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

        var organizationId = TryParseGuid(principal.FindFirstValue(AccessClaimTypes.OrganizationId));
        var tenantId = TryParseGuid(principal.FindFirstValue(AccessClaimTypes.TenantId));

        if (principal.HasClaim(AccessClaimTypes.IsPlatformAdmin, "true"))
        {
            // Platform authority and the selected organization are independent.
            // A selected platform view stays organization-limited for ordinary
            // data queries while retaining access to platform-only endpoints.
            if (organizationId is null)
            {
                return new AccessScope(null, null, [UserRole.PlatformAdmin]);
            }

            return new AccessScope(organizationId, tenantId, roles)
            {
                IsPlatformAdmin = true
            };
        }

        if (organizationId is null)
        {
            return null;
        }

        return new AccessScope(organizationId, tenantId, roles);
    }

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;
}
