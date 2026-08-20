// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Integrations;

public sealed record ProviderDescriptor(
    string Id,
    string DisplayName,
    IReadOnlyList<ProviderSettingDescriptor> Settings);

public sealed record ProviderSettingDescriptor(
    string Key,
    string Label,
    string InputType,
    bool Required,
    string? Placeholder = null);
