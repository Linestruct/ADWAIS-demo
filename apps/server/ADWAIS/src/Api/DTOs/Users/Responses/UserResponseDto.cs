// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

public record UserResponseDto(
    Guid Id,
    string Name,
    string? Email,
    UserRole Role,
    Guid? OrganizationId = null,
    string? OrganizationName = null,
    Guid? TenantId = null,
    bool IsPlatformAdmin = false
);
