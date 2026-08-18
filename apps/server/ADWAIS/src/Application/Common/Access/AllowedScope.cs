// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

/// <summary>
/// An organization or tenant context a user may operate in, with the roles
/// valid inside that context.
/// </summary>
public sealed record AllowedScope(Guid OrganizationId, Guid? TenantId, IReadOnlyCollection<UserRole> Roles);
