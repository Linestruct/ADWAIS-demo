// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Adwais.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Adwais.Infrastructure.Security;

/// <summary>
/// Intercepts incoming ClaimsPrincipals to auto-provision OIDC users and map them to local roles.
/// </summary>
public class LocalUserClaimsTransformation(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IConfiguration configuration) : IClaimsTransformation
{
    private readonly IDbContextFactory<AnalyticsDbContext> _dbContextFactory = dbContextFactory;
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
            return principal;
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
                // Reconcile accounts provisioned by a previous identity format/provider by email.
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
                // Auto-provision user with default Employee role
                user = new User
                {
                    Id = Guid.NewGuid(),
                    ExternalSubjectId = subjectId,
                    Name = name,
                    Email = email,
                    Role = UserRole.Employee
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
        }

        // Append role and scope claims using a cloned principal to ensure thread-safety/immutability
        var clone = principal.Clone();
        
        if (clone.Identity is ClaimsIdentity primaryIdentity)
        {
            var existingNameIds = primaryIdentity.FindAll(ClaimTypes.NameIdentifier).ToList();
            foreach (var claim in existingNameIds)
            {
                primaryIdentity.RemoveClaim(claim);
            }
        }

        var memberships = await db.UserAccesses
            .AsNoTracking()
            .Where(access => access.UserId == user.Id)
            .ToListAsync();

        var localIdentity = new ClaimsIdentity("LocalDatabaseRoles");
        if (memberships.Count > 0)
        {
            foreach (var role in memberships.Select(membership => membership.Role).Distinct())
            {
                localIdentity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
            }

            var scope = AccessScopeResolver.Resolve(memberships);
            if (scope?.OrganizationId is { } organizationId)
            {
                localIdentity.AddClaim(new Claim(AccessClaimTypes.OrganizationId, organizationId.ToString()));
            }
            if (scope?.TenantId is { } tenantId)
            {
                localIdentity.AddClaim(new Claim(AccessClaimTypes.TenantId, tenantId.ToString()));
            }
        }
        else
        {
            localIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        }

        localIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        clone.AddIdentity(localIdentity);

        return clone;
    }
}
