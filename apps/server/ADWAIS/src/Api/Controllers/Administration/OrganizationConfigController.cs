// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Api.Extensions;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Administration;

/// <summary>
/// Per-organization configuration. Staff read and write their own
/// organization; platform admins address any organization by id.
/// </summary>
[ApiController]
[Route("api/organizations")]
public class OrganizationConfigController(
    IOrganizationConfigService configService,
    ICurrentAccess currentAccess) : ControllerBase
{
    private readonly IOrganizationConfigService _configService = configService;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    private bool CanRead(Guid organizationId)
    {
        var filter = OrganizationFilter.From(_currentAccess.Scope);
        return !filter.Denied && (filter.OrganizationId is null || filter.OrganizationId.Value == organizationId);
    }

    [HttpGet("{id:guid}/config")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<OrganizationConfigDto>> GetConfig(Guid id, CancellationToken ct)
    {
        if (!CanRead(id))
        {
            return NotFound();
        }

        var config = await _configService.GetConfigAsync(id, ct);
        return config is null ? NotFound() : Ok(config);
    }

    [HttpPatch("{id:guid}/config")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<OrganizationConfigDto>> UpdateConfig(
        Guid id,
        [FromBody] UpdateOrganizationConfigRequestDto request,
        CancellationToken ct)
    {
        if (!CanRead(id))
        {
            return NotFound();
        }

        var result = await _configService.UpdateConfigAsync(id, request, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpGet("me/config")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<OrganizationConfigDto>> GetMyConfig(CancellationToken ct)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        if (orgId is null)
        {
            return NotFound();
        }

        var config = await _configService.GetConfigAsync(orgId.Value, ct);
        return config is null ? NotFound() : Ok(config);
    }

    [HttpPatch("me/config")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<OrganizationConfigDto>> UpdateMyConfig(
        [FromBody] UpdateOrganizationConfigRequestDto request,
        CancellationToken ct)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        if (orgId is null)
        {
            return Forbid();
        }

        var result = await _configService.UpdateConfigAsync(orgId.Value, request, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
}
