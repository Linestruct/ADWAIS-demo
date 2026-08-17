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
                // Users are provisioned by an admin. Unknown subjects get no claims.
                return principal;
            }
        }

        var memberships = await db.UserAccesses
            .AsNoTracking()
            .Where(access => access.UserId == user.Id)
            .ToListAsync();

        var scope = AccessScopeResolver.Resolve(memberships);
        if (scope is null)
        {
            // A provisioned user without membership rows gets no claims.
            return principal;
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

        var localIdentity = AccessClaimsBuilder.Build(user.Id, scope);
        clone.AddIdentity(localIdentity);

        return clone;
    }
}
