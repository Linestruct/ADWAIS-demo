// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;

namespace Adwais.Application.Common.Access;

/// <summary>
/// Builds the local database identity from a resolved access scope. The
/// claims transformation and development tooling share this builder so claim
/// semantics live in one place.
/// </summary>
public static class AccessClaimsBuilder
{
    public const string LocalDatabaseIdentity = "LocalDatabaseRoles";

    public static ClaimsIdentity Build(Guid userId, AccessScope scope)
    {
        var identity = new ClaimsIdentity(LocalDatabaseIdentity);
        foreach (var role in scope.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
        }

        if (scope.IsPlatformAdmin)
        {
            identity.AddClaim(new Claim(AccessClaimTypes.IsPlatformAdmin, "true"));
        }
        else
        {
            identity.AddClaim(new Claim(AccessClaimTypes.OrganizationId, scope.OrganizationId!.Value.ToString()));
            if (scope.TenantId is { } tenantId)
            {
                identity.AddClaim(new Claim(AccessClaimTypes.TenantId, tenantId.ToString()));
            }
        }

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        return identity;
    }
}
