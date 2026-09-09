// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

/// <summary>
/// An organization or tenant context a user may operate in, with the roles
/// valid inside that context.
/// </summary>
public sealed record AllowedScope(Guid OrganizationId, Guid? TenantId, IReadOnlyCollection<UserRole> Roles);
