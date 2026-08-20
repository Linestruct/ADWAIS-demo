// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

/// <summary>
/// Request data for pre-provisioning an application user.
/// </summary>
/// <param name="Email">The user's email address used to match the external identity.</param>
/// <param name="Role">The application role assigned to the user.</param>
public record CreateUserRequestDto(
    string Email,
    UserRole Role
);


