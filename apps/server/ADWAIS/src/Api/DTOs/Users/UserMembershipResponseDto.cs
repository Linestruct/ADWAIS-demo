// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;
using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

public record UserMembershipResponseDto(
    [property: Required] Guid Id,
    [property: Required] Guid UserId,
    Guid? OrganizationId,
    string? OrganizationName,
    [property: Required] UserRole Role);