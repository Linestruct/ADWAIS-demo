// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Organizations;

public record OrganizationSummaryResponseDto(Guid Id, string Name, int MemberCount, int MonitorCount);
