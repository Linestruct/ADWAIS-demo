// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
    /// Stores the enum as its name string. Callers keep chainable
    /// configuration such as max length and required flags.
    /// </summary>
    public static PropertyBuilder<TEnum> StoreAsString<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum
    {
        builder.HasConversion<string>();
        return builder;
    }

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