// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Service managing system users database interactions. Reads and writes are
/// scoped by the caller's access scope: organization staff see only members of
/// their organization, platform admins see everyone.
/// </summary>
public class UserService(IApplicationDbContext dbContext, ICurrentAccess currentAccess) : IUserService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    private OrganizationFilter Filter => OrganizationFilter.From(_currentAccess.Scope);

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return [];
        }

        var query = _dbContext.Users.AsNoTracking();
        if (filter.OrganizationId is { } orgId)
        {
            query = query.Where(user => _dbContext.UserAccesses
                .Any(access => access.UserId == user.Id && access.OrganizationId == orgId));
        }

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return null;
        }

        var query = _dbContext.Users.AsNoTracking().Where(user => user.Id == id);
        if (filter.OrganizationId is { } orgId)
        {
            query = query.Where(user => _dbContext.UserAccesses
                .Any(access => access.UserId == user.Id && access.OrganizationId == orgId));
        }

        return await query.SingleOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByExternalSubjectIdAsync(string externalSubjectId, CancellationToken ct)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.ExternalSubjectId == externalSubjectId, ct);
    }

    /// <inheritdoc />
    public async Task<User> CreateUserAsync(string email, UserRole role, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            throw new UnauthorizedAccessException("The current scope cannot create users.");
        }

        var organizationId = filter.OrganizationId ?? AnalyticsDbContext.DefaultOrganizationGuid;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = email, // Set Name to Email initially as placeholder
            Role = role
        };

        _dbContext.Users.Add(user);
        _dbContext.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = organizationId,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    /// <inheritdoc />
    public async Task<User?> UpdateUserAsync(Guid id, string? name, UserRole? role, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return null;
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
        {
            return null;
        }

        if (filter.OrganizationId is { } orgId && !await IsMemberOfOrganizationAsync(user.Id, orgId, ct))
        {
            return null;
        }

        if (name != null)
        {
            user.Name = name;
        }

        if (role.HasValue)
        {
            user.Role = role.Value;
            var memberships = await _dbContext.UserAccesses
                .Where(access => access.UserId == user.Id
                    && (filter.OrganizationId == null || access.OrganizationId == filter.OrganizationId))
                .ToListAsync(ct);
            foreach (var membership in memberships)
            {
                membership.Role = role.Value;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return false;
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
        {
            return false;
        }

        if (filter.OrganizationId is { } orgId)
        {
            var membershipOrgIds = await _dbContext.UserAccesses
                .Where(access => access.UserId == user.Id)
                .Select(access => access.OrganizationId)
                .ToListAsync(ct);
            if (membershipOrgIds.Count == 0 || membershipOrgIds.Any(membershipOrgId => membershipOrgId != orgId))
            {
                // Deleting cascades across every organization. Refuse when the
                // user reaches beyond this organization or is invisible here.
                return false;
            }
        }

        var memberships = await _dbContext.UserAccesses
            .Where(access => access.UserId == user.Id)
            .ToListAsync(ct);
        _dbContext.UserAccesses.RemoveRange(memberships);
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken ct)
        => await _dbContext.UserAccesses
            .AnyAsync(access => access.UserId == userId && access.OrganizationId == organizationId, ct);
}
