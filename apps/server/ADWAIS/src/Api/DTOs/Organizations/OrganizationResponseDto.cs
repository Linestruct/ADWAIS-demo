// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;

namespace Adwais.Api.DTOs.Organizations;

public record OrganizationResponseDto(
    [property: Required] Guid Id,
    [property: Required] string Name);
