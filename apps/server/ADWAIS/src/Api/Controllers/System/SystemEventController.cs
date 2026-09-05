// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Api.Controllers.System;

/// <summary>
/// Provides access to system-wide audit events and logs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemEventController(IApplicationDbContext dbContext) : ControllerBase
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    /// <summary>
    /// Retrieves a list of recent system events, with optional filtering.
    /// </summary>
    /// <param name="take">Number of events to retrieve (default 50).</param>
    /// <param name="minLevel">Minimum event level to include (e.g., Information, Warning, Error).</param>
    /// <param name="tenantId">Filter events related to a specific tenant.</param>
    /// <returns>A list of system events.</returns>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<SystemEvent>>> GetEvents(
        [FromQuery] int take = 50, 
        [FromQuery] SystemEventLevel? minLevel = null,
        [FromQuery] Guid? tenantId = null)
    {
        var db = _dbContext;
        
        var query = db.SystemEvents
            .AsNoTracking()
            .Include(e => e.Tenant)
            .OrderByDescending(e => e.Timestamp)
            .AsQueryable();

        if (minLevel.HasValue)
        {
            query = query.Where(e => e.Level >= minLevel.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        var events = await query.Take(take).ToListAsync();
        return Ok(events);
    }

    /// <summary>
    /// Deletes system events older than a specified number of days.
    /// </summary>
    /// <param name="olderThanDays">Delete events older than this many days (default 30).</param>
    /// <returns>The number of deleted events.</returns>
    [HttpDelete("clear")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ClearEvents([FromQuery] int olderThanDays = 30)
    {
        var db = _dbContext;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        
        var count = await db.SystemEvents
            .Where(e => e.Timestamp < cutoff)
            .ExecuteDeleteAsync();

        return Ok(new { DeletedCount = count });
    }
}
