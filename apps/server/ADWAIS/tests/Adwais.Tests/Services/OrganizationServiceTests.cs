// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Adwais.Tests.Services;

public class OrganizationServiceTests : IDisposable
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<IRecurringJobManager> _jobsMock;
    private readonly OrganizationService _service;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _otherOrgId = Guid.NewGuid();

    public OrganizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(options);
        _jobsMock = new Mock<IRecurringJobManager>();
        _service = new OrganizationService(_dbContext, _jobsMock.Object);

        _dbContext.Organizations.AddRange(
            new Organization { Id = _orgId, Name = "Acme", CreatedAt = DateTimeOffset.UtcNow },
            new Organization { Id = _otherOrgId, Name = "Other", CreatedAt = DateTimeOffset.UtcNow });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateOrganizationAsync_CreatesRow()
    {
        var org = await _service.CreateOrganizationAsync("NewCo", CancellationToken.None);

        Assert.NotEqual(Guid.Empty, org.Id);
        Assert.Equal("NewCo", org.Name);
        Assert.NotNull(await _dbContext.Organizations.SingleOrDefaultAsync(o => o.Id == org.Id));
    }

    [Fact]
    public async Task GetOrganizationAsync_ReturnsNull_WhenMissing()
    {
        Assert.Null(await _service.GetOrganizationAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.NotNull(await _service.GetOrganizationAsync(_orgId, CancellationToken.None));
    }

    [Fact]
    public async Task RenameOrganizationAsync_RenamesExistingRow()
    {
        Assert.True(await _service.RenameOrganizationAsync(_orgId, "Renamed", CancellationToken.None));
        Assert.Equal("Renamed", (await _dbContext.Organizations.SingleAsync(o => o.Id == _orgId)).Name);
        Assert.False(await _service.RenameOrganizationAsync(Guid.NewGuid(), "Nope", CancellationToken.None));
    }

    [Fact]
    public async Task GetOrganizationSummariesAsync_CountsMembersAndMonitors()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, OrganizationId = _orgId, Name = "Store", Type = TenantType.B2B });
        _dbContext.Monitors.Add(new UptimeMonitor { Id = 1, TenantId = tenantId, Name = "M1", Url = "https://m1.example" });
        _dbContext.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), OrganizationId = _orgId,
            Role = UserRole.Admin, CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var summaries = await _service.GetOrganizationSummariesAsync(CancellationToken.None);

        Assert.Equal(2, summaries.Count);
        var acme = summaries.Single(s => s.Id == _orgId);
        Assert.Equal(1, acme.MemberCount);
        Assert.Equal(1, acme.MonitorCount);
        Assert.Equal(0, summaries.Single(s => s.Id == _otherOrgId).MemberCount);
    }

    [Fact]
    public async Task DeleteOrganizationAsync_RemovesFullGraphAndJobs()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var day = new DateTimeOffset(now.Date, TimeSpan.Zero);
        _dbContext.Tenants.AddRange(
            new Tenant { Id = tenantId, OrganizationId = _orgId, Name = "Store", Type = TenantType.B2B },
            new Tenant { Id = otherTenantId, OrganizationId = _otherOrgId, Name = "Kept", Type = TenantType.B2C });
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = 11, TenantId = tenantId, Name = "Gone", Url = "https://gone.example" },
            new UptimeMonitor { Id = 12, TenantId = otherTenantId, Name = "Kept", Url = "https://kept.example" });
        _dbContext.Orders.AddRange(
            NewOrder(tenantId, now), NewOrder(otherTenantId, now));
        _dbContext.ResponseTimes.AddRange(
            new ResponseTime { MonitorId = 11 }, new ResponseTime { MonitorId = 12 });
        _dbContext.MonitorAvailabilities.AddRange(
            new MonitorAvailability { MonitorId = 11, Date = day }, new MonitorAvailability { MonitorId = 12, Date = day });
        _dbContext.DailyTenantRollups.AddRange(
            NewTenantRollup(tenantId, day), NewTenantRollup(otherTenantId, day));
        _dbContext.DailyGlobalRollups.AddRange(
            NewGlobalRollup(_orgId, day), NewGlobalRollup(_otherOrgId, day));
        _dbContext.DailyLatencyTenantRollups.Add(new DailyLatencyTenantRollup { Date = day, OrganizationId = _orgId, TenantId = tenantId });
        _dbContext.DailyLatencyGlobalRollups.Add(new DailyLatencyGlobalRollup { Date = day, OrganizationId = _orgId });
        _dbContext.DailyLatencyMonitorRollups.Add(new DailyLatencyMonitorRollup { MonitorId = 11, Date = day, OrganizationId = _orgId });
        _dbContext.DailyAvailabilityTenantRollups.Add(new DailyAvailabilityTenantRollup { Date = day, OrganizationId = _orgId, TenantId = tenantId });
        _dbContext.DailyAvailabilityGlobalRollups.Add(new DailyAvailabilityGlobalRollup { Date = day, OrganizationId = _orgId });
        _dbContext.DailyAvailabilityMonitorRollups.Add(new DailyAvailabilityMonitorRollup { MonitorId = 11, Date = day, OrganizationId = _orgId });
        _dbContext.MaterializedViewDirty.Add(new MaterializedViewDirty { OrganizationId = _orgId });
        _dbContext.OrganizationConfigs.Add(new OrganizationConfig { OrganizationId = _orgId });
        var userId = Guid.NewGuid();
        _dbContext.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(), UserId = userId, OrganizationId = _orgId,
            Role = UserRole.Admin, CreatedAt = now
        });
        _dbContext.CalendarEvents.Add(new CalendarEvent
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, Title = "Gone"
        });
        _dbContext.CalendarSubscriptions.Add(new CalendarSubscription
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Gone", Url = "https://gone.example/feed"
        });
        var sourceId = Guid.NewGuid();
        _dbContext.FeedSources.Add(new FeedSource
        {
            Id = sourceId, OrganizationId = _orgId, Name = "Gone", Url = "https://gone.example/rss"
        });
        _dbContext.FeedItems.Add(new FeedItem
        {
            Id = Guid.NewGuid(), FeedSourceId = sourceId, Title = "Gone", Link = "https://gone.example/1",
            PublishDate = now.UtcDateTime
        });
        _dbContext.BulletinPosts.Add(new BulletinPost
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, Title = "Gone", Body = "Gone",
            CreatedAt = now.UtcDateTime
        });
        _dbContext.KioskDevices.Add(new KioskDevice
        {
            Id = Guid.NewGuid(), DeviceId = "kiosk-gone", OrganizationId = _orgId,
            ActivationCode = "G00001", ActivationCodeExpires = now, CreatedDate = now
        });
        await _dbContext.SaveChangesAsync();

        var result = await _service.DeleteOrganizationAsync(_orgId, CancellationToken.None);

        Assert.Equal(OrganizationDeleteResult.Deleted, result);
        Assert.Null(await _dbContext.Organizations.SingleOrDefaultAsync(o => o.Id == _orgId));
        Assert.Empty(await _dbContext.Tenants.Where(t => t.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.Monitors.Where(m => m.TenantId == tenantId).ToListAsync());
        Assert.Empty(await _dbContext.Orders.Where(o => o.TenantId == tenantId).ToListAsync());
        Assert.Empty(await _dbContext.ResponseTimes.Where(r => r.MonitorId == 11).ToListAsync());
        Assert.Empty(await _dbContext.MonitorAvailabilities.Where(m => m.MonitorId == 11).ToListAsync());
        Assert.Empty(await _dbContext.DailyTenantRollups.Where(r => r.TenantId == tenantId).ToListAsync());
        Assert.Empty(await _dbContext.DailyGlobalRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.DailyLatencyTenantRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.DailyLatencyGlobalRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.DailyLatencyMonitorRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.DailyAvailabilityTenantRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.DailyAvailabilityGlobalRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.DailyAvailabilityMonitorRollups.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.MaterializedViewDirty.Where(r => r.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.OrganizationConfigs.Where(c => c.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.UserAccesses.Where(a => a.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.CalendarEvents.Where(e => e.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.CalendarSubscriptions.Where(s => s.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.FeedSources.Where(s => s.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.FeedItems.Where(i => i.FeedSourceId == sourceId).ToListAsync());
        Assert.Empty(await _dbContext.BulletinPosts.Where(p => p.OrganizationId == _orgId).ToListAsync());
        Assert.Empty(await _dbContext.KioskDevices.Where(d => d.OrganizationId == _orgId).ToListAsync());

        Assert.NotNull(await _dbContext.Organizations.SingleOrDefaultAsync(o => o.Id == _otherOrgId));
        Assert.NotNull(await _dbContext.Tenants.SingleOrDefaultAsync(t => t.Id == otherTenantId));
        Assert.NotNull(await _dbContext.Monitors.SingleOrDefaultAsync(m => m.Id == 12));
        Assert.NotNull(await _dbContext.Orders.SingleOrDefaultAsync(o => o.TenantId == otherTenantId));

        foreach (var kind in Enum.GetValues<Adwais.Application.Common.Jobs.RecurringJobKind>()
                     .Where(Adwais.Application.Common.Jobs.RecurringJobId.IsOrganizationScoped))
        {
            var jobId = Adwais.Application.Common.Jobs.RecurringJobId.For(kind, _orgId);
            _jobsMock.Verify(j => j.RemoveIfExists(jobId), Times.Once);
        }
        _jobsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteOrganizationAsync_RefusesDefaultOrganization()
    {
        var result = await _service.DeleteOrganizationAsync(
            IApplicationDbContext.DefaultOrganizationGuid, CancellationToken.None);

        Assert.Equal(OrganizationDeleteResult.RefusedDefaultOrganization, result);
        _jobsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteOrganizationAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteOrganizationAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(OrganizationDeleteResult.NotFound, result);
        _jobsMock.VerifyNoOtherCalls();
    }

    private static Order NewOrder(Guid tenantId, DateTimeOffset createdDate) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        OrderNumber = $"order-{Guid.NewGuid()}",
        CreatedDate = createdDate,
        TotalValueIncVat = 10m,
        TotalValueExcVat = 10m,
        OrderState = OrderState.Completed,
    };

    private static DailyFinancialTenantRollup NewTenantRollup(Guid tenantId, DateTimeOffset createdDate) => new()
    {
        CreatedDate = createdDate,
        OrganizationId = Guid.NewGuid(),
        TenantId = tenantId,
        Volume = 1,
        Revenue = 10m
    };

    private static DailyFinancialGlobalRollup NewGlobalRollup(Guid organizationId, DateTimeOffset createdDate) => new()
    {
        CreatedDate = createdDate,
        OrganizationId = organizationId,
        GlobalVolume = 1,
        GlobalRevenue = 10m
    };

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
