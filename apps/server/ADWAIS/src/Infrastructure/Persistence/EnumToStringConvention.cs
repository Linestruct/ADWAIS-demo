// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Adwais.Infrastructure.Persistence;

/// <summary>
/// Stores every persisted enum by name unless a more specific conversion is
/// configured. Database validity remains an explicit table constraint.
/// </summary>
public sealed class EnumToStringConvention : IPropertyAddedConvention
{
    public void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
    {
        var propertyType = Nullable.GetUnderlyingType(propertyBuilder.Metadata.ClrType)
            ?? propertyBuilder.Metadata.ClrType;

        if (propertyType.IsEnum)
            propertyBuilder.HasConversion(typeof(string));
    }
}
