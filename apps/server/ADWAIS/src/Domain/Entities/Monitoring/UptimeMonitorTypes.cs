// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities.Monitoring;

public static class UptimeMonitorTypes
{
    public const string Http = "HTTP";

    public static string Normalize(string? type) =>
        string.IsNullOrWhiteSpace(type) ? Http : type.Trim().ToUpperInvariant();
}
