// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Tenants;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Api.Controllers.Administration;

/// <summary>
/// Manages tenant lifecycle and configuration.
/// </summary>
[ApiController]
[Route("api/tenants")]
public class TenantController(
    IApplicationDbContext dbContext,
    IMonitorOrchestrationService monitorService,
    IEnumerable<IOrderSource> orderSources) : ControllerBase
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IMonitorOrchestrationService _monitorService = monitorService;
    private readonly IEnumerable<IOrderSource> _orderSources = orderSources;

    /// <summary>
    /// Retrieves tenants, optionally filtered by ID.
    /// </summary>
    /// <param name="id">Optional tenant ID to retrieve a single tenant.</param>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<TenantResponseDto>>> GetTenants([FromQuery] Guid? id)
    {
        var context = _dbContext;

        if (id.HasValue)
        {
            var tenant = await context.Tenants
                .AsNoTracking()
                .Include(t => t.Monitors)
                .Where(t => t.Id == id.Value)
                .SingleOrDefaultAsync();

            if (tenant == null) return Ok(Enumerable.Empty<TenantResponseDto>());

            return Ok(new[] { Map(tenant) });
        }

        var tenants = await context.Tenants
            .AsNoTracking()
            .Include(t => t.Monitors)
            .ToListAsync();

        return Ok(tenants.Select(Map));
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequestDto request)
    {
        var context = _dbContext;

        var provider = request.OrderProvider.Trim().ToLowerInvariant();
        var source = _orderSources.ForProvider(provider);
        string? settings;
        try
        {
            settings = request.OrderProviderSettings is null ? null : source.MergeSettings(null, request.OrderProviderSettings);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        if (request.OrderFetchingEnabled && !source.IsConfigured(settings))
            return BadRequest("Configured order provider settings are required when order fetching is enabled.");

        var tenant = new Tenant
        {
            Name = request.Name,
            Type = request.Type,
            OrderProvider = provider,
            OrderProviderSettings = settings,
            ImageUrl = request.ImageUrl,
            OrderFetchingEnabled = request.OrderFetchingEnabled
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTenants), new { id = tenant.Id }, Map(tenant));
    }

    /// <summary>
    /// Deletes a tenant and reassigns its monitors to the system tenant.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        if (id == IApplicationDbContext.SystemTenantGuid)
        {
            return BadRequest("Cannot delete the system tenant.");
        }

        var context = _dbContext;
        var tenant = await context.Tenants.FindAsync(id);
        if (tenant == null)
        {
            return NotFound();
        }
        
        await _monitorService.ReassignAllTenantMonitorsToSystemAsync(id);

        context.Tenants.Remove(tenant);
        await context.SaveChangesAsync();
        return NoContent();
    }
    
    /// <summary>
    /// Partially updates a tenant's configuration.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequestDto request)
    {
        var context = _dbContext;

        var tenant = await context.Tenants.FindAsync(id);
        if (tenant == null)
        {
            return NotFound();
        }
        if (request.Name is not null)
        {
            tenant.Name = request.Name.Trim();
        }
        var providerChanged = false;
        if (request.OrderProvider is not null)
        {
            tenant.OrderProvider = request.OrderProvider.Trim().ToLowerInvariant();
            providerChanged = true;
        }
        var source = _orderSources.ForProvider(tenant.OrderProvider);
        if (providerChanged) tenant.OrderProviderSettings = null;
        if (request.OrderProviderSettings is not null)
        {
            try
            {
                tenant.OrderProviderSettings = source.MergeSettings(tenant.OrderProviderSettings, request.OrderProviderSettings);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        if (request.ImageUrl is not null)
        {
            tenant.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        }
        if (request.OrderFetchingEnabled.HasValue)
        {
            tenant.OrderFetchingEnabled = request.OrderFetchingEnabled.Value;
        }
        if (request.Type.HasValue)
        {
            tenant.Type = request.Type.Value;
        }

        if (tenant.OrderFetchingEnabled && !source.IsConfigured(tenant.OrderProviderSettings))
            return BadRequest("Configured order provider settings are required when order fetching is enabled.");

        await context.SaveChangesAsync();

        return Ok(Map(tenant));
    }

    private TenantResponseDto Map(Tenant tenant)
    {
        var source = _orderSources.ForProvider(tenant.OrderProvider);
        return new TenantResponseDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Type = tenant.Type,
            OrderProvider = tenant.OrderProvider,
            OrderProviderSettings = source.GetPublicSettings(tenant.OrderProviderSettings),
            OrderProviderConfiguredSecretKeys = source.GetConfiguredSecretKeys(tenant.OrderProviderSettings),
            ImageUrl = tenant.ImageUrl,
            CurrentlyFetching = tenant.CurrentlyFetching,
            FetchedFrom = tenant.FetchedFrom,
            FetchedUntil = tenant.FetchedUntil,
            LastPolled = tenant.LastPolled,
            OrderFetchingEnabled = tenant.OrderFetchingEnabled,
            MonitorCount = tenant.Monitors.Count,
            LastSyncError = tenant.LastSyncError,
            HasOrderProviderSettings = source.IsConfigured(tenant.OrderProviderSettings)
        };
    }
}
