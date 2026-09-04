// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Jobs;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Organization lifecycle. Deletes are explicit per table so the cascade
/// behaves the same on every database provider.
/// </summary>
public class OrganizationService(
    IApplicationDbContext dbContext,
    IRecurringJobManager recurringJobs) : IOrganizationService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IRecurringJobManager _recurringJobs = recurringJobs;

    /// <inheritdoc />
    public async Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken ct = default) =>
        await _dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganizationSummary>> GetOrganizationSummariesAsync(CancellationToken ct = default)
    {
        var orgs = await _dbContext.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new { o.Id, o.Name })
            .ToListAsync(ct);
        var ids = orgs.Select(o => o.Id).ToList();

        var memberCounts = (await _dbContext.UserAccesses
                .Where(a => a.OrganizationId != null && ids.Contains(a.OrganizationId.Value))
                .Select(a => a.OrganizationId!.Value)
                .ToListAsync(ct))
            .GroupBy(orgId => orgId)
            .ToDictionary(g => g.Key, g => g.Count());
        var monitorCounts = (await _dbContext.Monitors
                .Where(m => m.Tenant != null && ids.Contains(m.Tenant.OrganizationId))
                .Select(m => m.Tenant!.OrganizationId)
                .ToListAsync(ct))
            .GroupBy(orgId => orgId)
            .ToDictionary(g => g.Key, g => g.Count());

        return orgs
            .Select(o => new OrganizationSummary(
                o.Id,
                o.Name,
                memberCounts.GetValueOrDefault(o.Id),
                monitorCounts.GetValueOrDefault(o.Id)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Organization> CreateOrganizationAsync(string name, CancellationToken ct = default)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Organizations.Add(org);
        await _dbContext.SaveChangesAsync(ct);
        return org;
    }

    /// <inheritdoc />
    public async Task<bool> RenameOrganizationAsync(Guid id, string name, CancellationToken ct = default)
    {
        var org = await _dbContext.Organizations.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (org is null)
            return false;

        org.Name = name;
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<OrganizationDeleteResult> DeleteOrganizationAsync(Guid id, CancellationToken ct = default)
    {
        if (id == IApplicationDbContext.DefaultOrganizationGuid)
            return OrganizationDeleteResult.RefusedDefaultOrganization;

        var exists = await _dbContext.Organizations.AnyAsync(o => o.Id == id, ct);
        if (!exists)
            return OrganizationDeleteResult.NotFound;

        // Remove per-org schedules first: if the data deletes fail, the
        // bootstrap recreates the jobs while the org still exists.
        foreach (var kind in Enum.GetValues<RecurringJobKind>().Where(RecurringJobId.IsOrganizationScoped))
            _recurringJobs.RemoveIfExists(RecurringJobId.For(kind, id));

        var tenantIds = await _dbContext.Tenants
            .Where(t => t.OrganizationId == id)
            .Select(t => t.Id)
            .ToListAsync(ct);
        var monitorIds = await _dbContext.Monitors
            .Where(m => tenantIds.Contains(m.TenantId))
            .Select(m => m.Id)
            .ToListAsync(ct);
        var sourceIds = await _dbContext.FeedSources
            .Where(s => s.OrganizationId == id)
            .Select(s => s.Id)
            .ToListAsync(ct);

        _dbContext.ResponseTimes.RemoveRange(await _dbContext.ResponseTimes.Where(r => monitorIds.Contains(r.MonitorId)).ToListAsync(ct));
        _dbContext.MonitorAvailabilities.RemoveRange(await _dbContext.MonitorAvailabilities.Where(m => monitorIds.Contains(m.MonitorId)).ToListAsync(ct));
        _dbContext.DailyLatencyMonitorRollups.RemoveRange(await _dbContext.DailyLatencyMonitorRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.DailyAvailabilityMonitorRollups.RemoveRange(await _dbContext.DailyAvailabilityMonitorRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.Orders.RemoveRange(await _dbContext.Orders.Where(o => tenantIds.Contains(o.TenantId)).ToListAsync(ct));
        _dbContext.Monitors.RemoveRange(await _dbContext.Monitors.Where(m => monitorIds.Contains(m.Id)).ToListAsync(ct));
        _dbContext.DailyTenantRollups.RemoveRange(await _dbContext.DailyTenantRollups.Where(r => tenantIds.Contains(r.TenantId)).ToListAsync(ct));
        _dbContext.DailyGlobalRollups.RemoveRange(await _dbContext.DailyGlobalRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.DailyLatencyTenantRollups.RemoveRange(await _dbContext.DailyLatencyTenantRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.DailyLatencyGlobalRollups.RemoveRange(await _dbContext.DailyLatencyGlobalRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.DailyAvailabilityTenantRollups.RemoveRange(await _dbContext.DailyAvailabilityTenantRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.DailyAvailabilityGlobalRollups.RemoveRange(await _dbContext.DailyAvailabilityGlobalRollups.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.MaterializedViewDirty.RemoveRange(await _dbContext.MaterializedViewDirty.Where(r => r.OrganizationId == id).ToListAsync(ct));
        _dbContext.Tenants.RemoveRange(await _dbContext.Tenants.Where(t => t.OrganizationId == id).ToListAsync(ct));
        _dbContext.OrganizationConfigs.RemoveRange(await _dbContext.OrganizationConfigs.Where(c => c.OrganizationId == id).ToListAsync(ct));
        _dbContext.UserAccesses.RemoveRange(await _dbContext.UserAccesses.Where(a => a.OrganizationId == id).ToListAsync(ct));
        _dbContext.CalendarEvents.RemoveRange(await _dbContext.CalendarEvents.Where(e => e.OrganizationId == id).ToListAsync(ct));
        _dbContext.CalendarSubscriptions.RemoveRange(await _dbContext.CalendarSubscriptions.Where(s => s.OrganizationId == id).ToListAsync(ct));
        _dbContext.BulletinPosts.RemoveRange(await _dbContext.BulletinPosts.Where(p => p.OrganizationId == id).ToListAsync(ct));
        _dbContext.FeedItems.RemoveRange(await _dbContext.FeedItems.Where(i => i.FeedSourceId != null && sourceIds.Contains(i.FeedSourceId.Value)).ToListAsync(ct));
        _dbContext.FeedSources.RemoveRange(await _dbContext.FeedSources.Where(s => s.OrganizationId == id).ToListAsync(ct));
        _dbContext.KioskDevices.RemoveRange(await _dbContext.KioskDevices.Where(d => d.OrganizationId == id).ToListAsync(ct));
        _dbContext.Organizations.RemoveRange(await _dbContext.Organizations.Where(o => o.Id == id).ToListAsync(ct));

        await _dbContext.SaveChangesAsync(ct);
        return OrganizationDeleteResult.Deleted;
    }
}
