// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

/// <summary>
/// The visibility a principal has, with the roles valid inside that scope.
/// A null OrganizationId means the scope is platform-wide. Platform authority
/// may coexist with a selected organization so platform-only endpoints remain
/// available while ordinary data queries stay organization-limited.
/// </summary>
public sealed record AccessScope(
    Guid? OrganizationId,
    Guid? TenantId,
    IReadOnlyCollection<UserRole> Roles)
{
    /// <summary>
    /// Whether the principal has platform administration authority. This can
    /// remain true while OrganizationId identifies the selected organization.
    /// </summary>
    public bool IsPlatformAdmin { get; init; } = Roles.Contains(UserRole.PlatformAdmin);
    public bool IsTenantRestricted => TenantId is not null;
}
