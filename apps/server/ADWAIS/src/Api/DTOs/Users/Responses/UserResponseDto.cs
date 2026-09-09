// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;
using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

public record UserResponseDto(
    [property: Required] Guid Id,
    [property: Required] string Name,
    string? Email,
    UserRole? Role,
    Guid? OrganizationId = null,
    string? OrganizationName = null,
    Guid? TenantId = null,
    bool IsPlatformAdmin = false
);
