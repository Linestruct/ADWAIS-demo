// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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

    /// <summary>
    /// Lists the membership rows of a user. Scoped: organization callers see
    /// only their own organization's rows; platform admins see everything.
    /// </summary>
    Task<IReadOnlyList<UserAccess>> GetUserMembershipsAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Adds a membership row for a user. Organization callers may only add
    /// rows inside their own organization. Platform admins may add rows in any
    /// organization and may create platform-admin rows (null organization).
    /// </summary>
    Task<UserAccess> AddUserMembershipAsync(Guid userId, Guid? organizationId, UserRole role, CancellationToken ct);

    /// <summary>
    /// Removes a membership row. Organization callers may only remove rows in
    /// their own organization. A caller cannot remove their own platform-admin
    /// row.
    /// </summary>
    Task<bool> RemoveUserMembershipAsync(Guid targetUserId, Guid membershipId, Guid callerUserId, CancellationToken ct);
}
