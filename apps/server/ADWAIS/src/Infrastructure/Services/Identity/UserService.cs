// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Adwais.Application.Interfaces;
using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Service managing system users database interactions.
/// </summary>
public class UserService(IApplicationDbContext dbContext) : IUserService
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id, ct);
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = email, // Set Name to Email initially as placeholder
            Role = role
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    /// <inheritdoc />
    public async Task<User?> UpdateUserAsync(Guid id, string? name, UserRole? role, CancellationToken ct)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
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
        }

        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
