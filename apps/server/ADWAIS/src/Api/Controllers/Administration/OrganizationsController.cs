// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.DTOs.Organizations;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Api.Controllers.Administration;

/// <summary>
/// Lists organizations. Platform admins see every organization. Everyone
/// else sees the organizations from their own membership rows.
/// </summary>
[ApiController]
[Route("api/organizations")]
public class OrganizationsController(
    IApplicationDbContext dbContext,
    ICurrentAccess currentAccess,
    IOrganizationService organizationService) : ControllerBase
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IOrganizationService _organizationService = organizationService;

    private bool CanRead(Guid organizationId)
    {
        var filter = OrganizationFilter.From(_currentAccess.Scope);
        return !filter.Denied && (filter.OrganizationId is null || filter.OrganizationId.Value == organizationId);
    }

    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<OrganizationResponseDto[]>> GetOrganizations(CancellationToken ct)
    {
        var filter = OrganizationFilter.From(_currentAccess.Scope);
        if (filter.Denied)
        {
            return Ok(Array.Empty<OrganizationResponseDto>());
        }

        if (filter.OrganizationId is null)
        {
            var all = await _dbContext.Organizations
                .AsNoTracking()
                .OrderBy(org => org.Name)
                .Select(org => new OrganizationResponseDto(org.Id, org.Name))
                .ToArrayAsync(ct);
            return Ok(all);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var membershipOrgIds = Guid.TryParse(userId, out var parsedUserId)
            ? await _dbContext.UserAccesses
                .AsNoTracking()
                .Where(access => access.UserId == parsedUserId && access.OrganizationId != null)
                .Select(access => access.OrganizationId!.Value)
                .Distinct()
                .ToListAsync(ct)
            : [];

        var visibleOrgIds = membershipOrgIds.Count > 0
            ? membershipOrgIds
            : [filter.OrganizationId.Value];

        var orgs = await _dbContext.Organizations
            .AsNoTracking()
            .Where(org => visibleOrgIds.Contains(org.Id))
            .OrderBy(org => org.Name)
            .Select(org => new OrganizationResponseDto(org.Id, org.Name))
            .ToArrayAsync(ct);
        return Ok(orgs);
    }

    /// <summary>
    /// Gets one organization. Platform admins address any organization;
    /// staff see their own organization only.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<OrganizationResponseDto>> GetOrganization(Guid id, CancellationToken ct)
    {
        if (!CanRead(id))
        {
            return NotFound();
        }

        var org = await _organizationService.GetOrganizationAsync(id, ct);
        return org is null
            ? NotFound()
            : Ok(new OrganizationResponseDto(org.Id, org.Name));
    }

    /// <summary>
    /// Creates an organization. Platform admins only. Configuration is
    /// created lazily on first edit.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<ActionResult<OrganizationResponseDto>> CreateOrganization(
        [FromBody] CreateOrganizationRequestDto request, CancellationToken ct)
    {
        var org = await _organizationService.CreateOrganizationAsync(request.Name, ct);
        return CreatedAtAction(nameof(GetOrganization), new { id = org.Id },
            new OrganizationResponseDto(org.Id, org.Name));
    }

    /// <summary>
    /// Renames an organization. Platform admins address any organization;
    /// organization admins rename their own organization only.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<OrganizationResponseDto>> RenameOrganization(
        Guid id, [FromBody] UpdateOrganizationRequestDto request, CancellationToken ct)
    {
        if (!CanRead(id))
        {
            return NotFound();
        }

        var renamed = await _organizationService.RenameOrganizationAsync(id, request.Name, ct);
        if (!renamed)
        {
            return NotFound();
        }

        var org = await _organizationService.GetOrganizationAsync(id, ct);
        return Ok(new OrganizationResponseDto(org!.Id, org.Name));
    }

    /// <summary>
    /// Hard-deletes an organization with its full data graph and per-org
    /// schedules. Platform admins only. The default organization is refused.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<IActionResult> DeleteOrganization(Guid id, CancellationToken ct)
    {
        var result = await _organizationService.DeleteOrganizationAsync(id, ct);
        return result switch
        {
            OrganizationDeleteResult.Deleted => NoContent(),
            OrganizationDeleteResult.RefusedDefaultOrganization =>
                Conflict("The default organization cannot be deleted."),
            _ => NotFound(),
        };
    }
}
