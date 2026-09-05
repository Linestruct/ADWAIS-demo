// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Adwais.Infrastructure.Persistence.Converters;

public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IDataProtectionProvider provider, ConverterMappingHints? mappingHints = null) 
        : base(
            v => v == null ? null : provider.CreateProtector("TenantSecrets").Protect(v),
            v => v == null ? null : provider.CreateProtector("TenantSecrets").Unprotect(v),
            mappingHints)
    {
    }
}
