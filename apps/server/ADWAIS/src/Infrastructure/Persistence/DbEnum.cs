// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Linq;

namespace Adwais.Infrastructure.Persistence;

/// <summary>
/// Derives the string storage shape of an enum from the enum type itself.
/// Keeps the database schema in lockstep with the enum class: renaming or
/// adding a value propagates to conversions, lengths, and check constraints
/// on the next migration scaffold.
/// </summary>
public static class DbEnum
{
    /// <summary>
    /// The length of the longest enum value name.
    /// </summary>
    public static int NameMaxLength<TEnum>()
        where TEnum : struct, Enum
        => Enum.GetNames<TEnum>().Max(name => name.Length);

    /// <summary>
    /// SQL for a check constraint that admits exactly the enum value names,
    /// for example: <c>"role" IN ('Admin', 'Viewer')</c>.
    /// </summary>
    public static string CheckConstraintSql<TEnum>(string column)
        where TEnum : struct, Enum
    {
        var values = string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"));
        return $"\"{column}\" IN ({values})";
    }
}
