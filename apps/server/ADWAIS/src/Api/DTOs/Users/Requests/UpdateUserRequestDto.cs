// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

/// <summary>
/// Request data for partially updating an application user.
/// </summary>
/// <param name="Name">The replacement display name, or <see langword="null"/> to keep the current name.</param>
/// <param name="Role">The replacement application role, or <see langword="null"/> to keep the current role.</param>
public record UpdateUserRequestDto(
    string? Name = null,
    UserRole? Role = null
);


