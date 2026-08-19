// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Adwais.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Adwais.Infrastructure.Security;

/// <summary>
/// Intercepts incoming ClaimsPrincipals to auto-provision OIDC users and map them to local roles.
/// </summary>
public class LocalUserClaimsTransformation(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor) : IClaimsTransformation
{
    private readonly IDbContextFactory<AnalyticsDbContext> _dbContextFactory = dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly string _kioskIssuer = configuration["Authentication:KioskJwtIssuer"] ?? "ADWAIS";

    /// <inheritdoc />
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity == null || !principal.Identity.IsAuthenticated)
        {
            return principal;
        }

        if (principal.FindFirst("iss")?.Value == _kioskIssuer)
        {
            return principal;
        }

        var subjectId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subjectId))
        {
            return WithoutAuthorityClaims(principal);
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var user = await db.Users.SingleOrDefaultAsync(u => u.ExternalSubjectId == subjectId);

        var name = principal.FindFirst("name")?.Value 
                   ?? "New User";
        var email = principal.FindFirst("email")?.Value
                     ?? principal.FindFirst("preferred_username")?.Value;

        if (user != null)
        {
            bool modified = false;
            if (user.Name != name)
            {
                user.Name = name;
                modified = true;
            }
            if (user.Email != email && !string.IsNullOrEmpty(email))
            {
                user.Email = email;
                modified = true;
            }
            if (modified)
            {
                await db.SaveChangesAsync();
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(email))
            {
                var lowerEmail = email.ToLowerInvariant();
                // Link accounts provisioned ahead of time by email.
                user = await db.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == lowerEmail);

                if (user != null)
                {
                    user.ExternalSubjectId = subjectId;
                    user.Name = name;
                    await db.SaveChangesAsync();
                }
            }

            if (user == null)
            {
                // Users are provisioned by an admin. Unknown subjects get no authority claims.
                return WithoutAuthorityClaims(principal);
            }
        }

        var memberships = await db.UserAccesses
            .AsNoTracking()
            .Where(access => access.UserId == user.Id)
            .ToListAsync();

        var resolution = AccessScopeResolver.ResolveAllowed(memberships);
        var requestedOrganizationId = TryParseGuid(
            _httpContextAccessor.HttpContext?.Request.Headers[AccessRequestHeaders.OrganizationId]);
        var requestedTenantId = TryParseGuid(
            _httpContextAccessor.HttpContext?.Request.Headers[AccessRequestHeaders.TenantId]);

        var scope = AccessScopeResolver.SelectEffective(resolution, requestedOrganizationId, requestedTenantId);
        if (scope is null)
        {
            // A provisioned user without a valid scope gets no authority claims.
            return WithoutAuthorityClaims(principal);
        }

        // Upstream identities never decide roles or scope. Scrub their authority
        // claims so the local identity is the only source, then attach it.
        var clone = WithoutAuthorityClaims(principal);
        clone.AddIdentity(AccessClaimsBuilder.Build(user.Id, scope));

        return clone;
    }

    /// <summary>
    /// Removes role, scope, and name identifier claims from every upstream
    /// identity. Identity claims such as name and email are kept. Used so a
    /// federated principal can never carry authority that membership did not grant.
    /// </summary>
    private static ClaimsPrincipal WithoutAuthorityClaims(ClaimsPrincipal principal)
    {
        var clone = principal.Clone();
        foreach (var identity in clone.Identities)
        {
            var roleClaimType = identity.RoleClaimType;
            var authorityClaims = identity.FindAll(claim =>
                    claim.Type is ClaimTypes.Role
                        or "role"
                        or "roles"
                        or ClaimTypes.NameIdentifier
                        or AccessClaimTypes.OrganizationId
                        or AccessClaimTypes.TenantId
                        or AccessClaimTypes.IsPlatformAdmin
                        || (!string.IsNullOrEmpty(roleClaimType) && claim.Type == roleClaimType))
                .ToList();
            foreach (var claim in authorityClaims)
            {
                identity.RemoveClaim(claim);
            }
        }

        return clone;
    }

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;
}
