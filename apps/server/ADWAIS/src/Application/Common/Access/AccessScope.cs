// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

/// <summary>
/// The visibility a principal has, with the roles valid inside that scope.
/// A null OrganizationId means the platform admin scope. A set TenantId means
/// the principal is restricted to one tenant.
/// </summary>
public sealed record AccessScope(
    Guid? OrganizationId,
    Guid? TenantId,
    IReadOnlyCollection<UserRole> Roles)
{
    /// <summary>
    /// A scope is platform-wide when it carries the explicit platform role.
    /// Platform scopes always have a null organization; org scopes never
    /// carry the platform role. The invariant is enforced on membership writes.
    /// </summary>
    public bool IsPlatformAdmin => Roles.Contains(UserRole.PlatformAdmin);
    public bool IsTenantRestricted => TenantId is not null;
}
