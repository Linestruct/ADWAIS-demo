// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.ComponentModel.DataAnnotations;

namespace Adwais.Api.DTOs.Organizations;

public record OrganizationResponseDto(
    [property: Required] Guid Id,
    [property: Required] string Name);
