// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using FluentResults;

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
    public async Task<Result<User>> CreateUserAsync(string email, UserRole role, Guid? organizationId = null, CancellationToken ct = default)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return Result.Fail<User>(ScopeDenied("create users"));
        }

        if (role == UserRole.TenantViewer)
        {
            return Result.Fail<User>(Validation("role", "Tenant viewers are added through memberships, not user creation."));
        }

        Guid? targetOrg;
        if (role == UserRole.PlatformAdmin)
        {
            if (filter.OrganizationId is not null)
                return Result.Fail<User>(ScopeDenied("create platform administrators"));
            if (organizationId.HasValue)
                return Result.Fail<User>(Validation("organizationId", "Platform admins do not belong to an organization."));
            targetOrg = null;
        }
        else if (organizationId.HasValue)
        {
            if (filter.OrganizationId is not null && filter.OrganizationId.Value != organizationId.Value)
                return Result.Fail<User>(ScopeDenied("create users in the requested organization"));
            var orgExists = await _dbContext.Organizations.AnyAsync(o => o.Id == organizationId.Value, ct);
            if (!orgExists)
                return Result.Fail<User>(new NotFoundError("Organization", organizationId.Value));
            targetOrg = organizationId.Value;
        }
        else if (filter.OrganizationId is { } callerOrgId)
        {
            targetOrg = callerOrgId;
        }
        else
        {
            return Result.Fail<User>(Validation("organizationId", "An organization is required to create this user."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = email // Set Name to Email initially as placeholder
        };

        _dbContext.Users.Add(user);
        _dbContext.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = targetOrg,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(ct);
        return Result.Ok(user);
    }

    /// <inheritdoc />
    public async Task<Result<User>> UpdateUserAsync(Guid id, string? name, UserRole? role, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return Result.Fail<User>(ScopeDenied("update users"));
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
        {
            return Result.Fail<User>(new NotFoundError("User", id));
        }

        if (filter.OrganizationId is { } orgId && !await IsMemberOfOrganizationAsync(user.Id, orgId, ct))
        {
            return Result.Fail<User>(ScopeDenied("update this user"));
        }

        if (name != null)
        {
            user.Name = name;
        }

        if (role.HasValue)
        {
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
        return Result.Ok(user);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(Guid id, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return Result.Fail(ScopeDenied("delete users"));
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
        {
            return Result.Fail(new NotFoundError("User", id));
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
                return Result.Fail(ScopeDenied("delete this user"));
            }
        }

        var memberships = await _dbContext.UserAccesses
            .Where(access => access.UserId == user.Id)
            .ToListAsync(ct);
        _dbContext.UserAccesses.RemoveRange(memberships);
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken ct)
        => await _dbContext.UserAccesses
            .AnyAsync(access => access.UserId == userId && access.OrganizationId == organizationId, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAccess>> GetUserMembershipsAsync(Guid userId, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return [];
        }

        var query = _dbContext.UserAccesses
            .AsNoTracking()
            .Include(access => access.Organization)
            .Where(access => access.UserId == userId);
        if (filter.OrganizationId is { } orgId)
        {
            query = query.Where(access => access.OrganizationId == orgId);
        }

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Result<UserAccess>> AddUserMembershipAsync(Guid userId, Guid? organizationId, UserRole role, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return Result.Fail<UserAccess>(ScopeDenied("manage memberships"));
        }
        if (role == UserRole.TenantViewer)
        {
            return Result.Fail<UserAccess>(Validation("role", "Tenant viewer memberships require a tenant and are not supported yet."));
        }

        // The platform role only exists on null-org rows, and null-org rows
        // only ever hold the platform role. Org rows never carry it.
        if (organizationId is null && role != UserRole.PlatformAdmin)
        {
            return Result.Fail<UserAccess>(Validation("organizationId", "Memberships without an organization must use the PlatformAdmin role."));
        }
        if (organizationId is not null && role == UserRole.PlatformAdmin)
        {
            return Result.Fail<UserAccess>(Validation("organizationId", "The PlatformAdmin role requires a membership without an organization."));
        }

        var userExists = await _dbContext.Users.AnyAsync(user => user.Id == userId, ct);
        if (!userExists)
        {
            return Result.Fail<UserAccess>(new NotFoundError("User", userId));
        }

        if (organizationId is null)
        {
            if (filter.OrganizationId is not null)
            {
                return Result.Fail<UserAccess>(ScopeDenied("create platform administrator memberships"));
            }
        }
        else
        {
            if (filter.OrganizationId is { } orgId && orgId != organizationId.Value)
            {
                return Result.Fail<UserAccess>(ScopeDenied("manage memberships in the requested organization"));
            }
            var organizationExists = await _dbContext.Organizations
                .AnyAsync(org => org.Id == organizationId.Value, ct);
            if (!organizationExists)
            {
                return Result.Fail<UserAccess>(new NotFoundError("Organization", organizationId.Value));
            }
        }

        var duplicate = await _dbContext.UserAccesses
            .AnyAsync(access => access.UserId == userId && access.OrganizationId == organizationId, ct);
        if (duplicate)
        {
            return Result.Fail<UserAccess>(new ConflictError("The user already belongs to that organization."));
        }

        var membership = new UserAccess
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.UserAccesses.Add(membership);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Ok(membership);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveUserMembershipAsync(Guid targetUserId, Guid membershipId, Guid callerUserId, CancellationToken ct)
    {
        var filter = Filter;
        if (filter.Denied)
        {
            return Result.Fail(ScopeDenied("manage memberships"));
        }

        var membership = await _dbContext.UserAccesses
            .SingleOrDefaultAsync(access => access.Id == membershipId, ct);
        if (membership == null)
        {
            return Result.Fail(new NotFoundError("Membership", membershipId));
        }

        if (filter.OrganizationId is { } orgId && membership.OrganizationId != orgId)
        {
            return Result.Fail(ScopeDenied("remove this membership"));
        }

        if (membership.OrganizationId is null && targetUserId == callerUserId)
        {
            return Result.Fail(ScopeDenied("remove your own platform administrator membership"));
        }

        _dbContext.UserAccesses.Remove(membership);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private ScopeDeniedError ScopeDenied(string required)
        => new(required, Filter.OrganizationId?.ToString() ?? "platform");

    private static ValidationError Validation(string field, string message)
        => new(new Dictionary<string, string[]> { [field] = [message] });
}
