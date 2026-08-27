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
    ICurrentAccess currentAccess) : ControllerBase
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;

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
}
