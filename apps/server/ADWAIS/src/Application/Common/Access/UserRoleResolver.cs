// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Collections.Generic;
using System.Linq;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

public static class UserRoleResolver
{
    /// <summary>
    /// Resolves a single representative role from a user's memberships.
    /// A platform membership always wins; otherwise the highest org role.
    /// Used for list views where one role label is shown.
    /// </summary>
    public static UserRole? Dominant(IEnumerable<UserAccess> memberships)
    {
        var rows = memberships.ToList();
        if (rows.Count == 0) return null;

        if (rows.Any(m => m.Role == UserRole.PlatformAdmin)) return UserRole.PlatformAdmin;

        return rows
            .Where(m => m.OrganizationId is not null)
            .Select(m => m.Role)
            .OrderByDescending(Precedence)
            .FirstOrDefault();
    }

    public static bool IsPlatformAdmin(IEnumerable<UserAccess> memberships)
        => memberships.Any(m => m.Role == UserRole.PlatformAdmin);

    private static int Precedence(UserRole role) => role switch
    {
        UserRole.Admin => 3,
        UserRole.Employee => 2,
        UserRole.Viewer => 1,
        _ => 0
    };
}