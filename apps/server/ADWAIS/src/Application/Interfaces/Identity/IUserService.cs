// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Provides administrative user management capabilities, decoupling database access from controllers.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves all users configured in the system.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A collection of all user records.</returns>
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves a single user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user record, or null if not found.</returns>
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves a single user by their external OIDC subject identifier.
    /// </summary>
    /// <param name="externalSubjectId">The subject identifier issued by the external identity provider.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user record, or null if not found.</returns>
    Task<User?> GetUserByExternalSubjectIdAsync(string externalSubjectId, CancellationToken ct);

    /// <summary>
    /// Creates a new user record.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="role">The access role assigned to the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The newly created user record.</returns>
    Task<User> CreateUserAsync(string email, UserRole role, CancellationToken ct);

    /// <summary>
    /// Updates an existing user record.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="name">The new name (optional).</param>
    /// <param name="role">The new role (optional).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user record, or null if the user was not found.</returns>
    Task<User?> UpdateUserAsync(Guid id, string? name, UserRole? role, CancellationToken ct);

    /// <summary>
    /// Deletes a user record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the user was deleted successfully, otherwise false.</returns>
    Task<bool> DeleteUserAsync(Guid id, CancellationToken ct);
}
