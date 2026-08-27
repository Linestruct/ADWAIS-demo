// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.ComponentModel.DataAnnotations;
using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Users;

public record AddUserMembershipRequestDto(
    Guid? OrganizationId,
    [property: Required] UserRole Role);