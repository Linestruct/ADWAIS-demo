// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.GlobalConfig;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IOrganizationConfigService
{
    Task<OrganizationConfigDto?> GetConfigAsync(CancellationToken ct = default);
    Task<OrganizationConfigDto?> GetConfigAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<OrganizationConfigDto>> UpdateConfigAsync(Guid organizationId, UpdateOrganizationConfigRequestDto request, CancellationToken ct = default);
}
