// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

public record CreateUserRequestDto(
    string Email,
    UserRole Role,
    Guid? OrganizationId = null
);


