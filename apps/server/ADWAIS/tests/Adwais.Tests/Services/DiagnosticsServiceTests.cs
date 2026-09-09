using Adwais.Application.Common.Access;
using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Adwais.Tests.Services;

public sealed class DiagnosticsServiceTests : IDisposable
{
    private readonly AnalyticsDbContext _db;
    private readonly Mock<ICurrentAccess> _access = new();
    private readonly Mock<ISystemHealthService> _health = new();
    private readonly DiagnosticsService _service;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _systemTenantA = Guid.NewGuid();

    public DiagnosticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AnalyticsDbContext(options);
        _health.Setup(service => service.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemHealthDto(
                "Healthy",
                new HangfireHealthDto("Healthy", 0, 0, 0, 0),
                new SyncHealthDto("Healthy", 0, 0, 0, null),
                null, null, null, null, null));
        _service = new DiagnosticsService(_db, _access.Object, _health.Object);

        _db.Organizations.AddRange(
            new Organization { Id = _orgA, Name = "Organization A" },
            new Organization { Id = _orgB, Name = "Organization B" });
        _db.Tenants.AddRange(
            new Tenant { Id = _tenantA, OrganizationId = _orgA, Name = "Tenant A", Type = TenantType.Mixed },
            new Tenant { Id = _tenantB, OrganizationId = _orgB, Name = "Tenant B", Type = TenantType.Mixed },
            new Tenant { Id = _systemTenantA, OrganizationId = _orgA, Name = "System A", Type = TenantType.Mixed, IsSystem = true });

        _db.PipelineRuns.AddRange(
            Run(_orgA, _tenantA, PipelineRunState.Failed, "A failed"),
            Run(_orgB, _tenantB, PipelineRunState.Succeeded, "B succeeded"),
            Run(_orgA, _systemTenantA, PipelineRunState.Failed, "System failed"),
            Run(_orgA, null, PipelineRunState.Failed, "Organization failed"));
        _db.SystemEvents.AddRange(
            Event(_orgA, _tenantA, SystemEventAudience.Organization, "A event", "secret details"),
            Event(_orgB, _tenantB, SystemEventAudience.Organization, "B event", "B details"),
            Event(_orgA, _systemTenantA, SystemEventAudience.Organization, "System event", "system details"),
            Event(null, null, SystemEventAudience.Platform, "Platform event", "platform details"));
        _db.SaveChanges();
    }

    [Fact]
    public async Task OrganizationQueries_ReturnOnlyOwnedSafeRecords()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Employee]));

        var runs = await _service.GetRunsAsync(_orgA, null, 100);
        var events = await _service.GetEventsAsync(_orgA, null, 100, null);

        Assert.True(runs.IsSuccess);
        Assert.Equal(2, runs.Value.Count);
        Assert.All(runs.Value, run => Assert.Equal(_orgA, run.OrganizationId));
        Assert.DoesNotContain(runs.Value, run => run.TenantId == _systemTenantA);

        Assert.True(events.IsSuccess);
        Assert.Single(events.Value);
        Assert.Equal("A event", events.Value[0].Message);
        Assert.DoesNotContain("secret", events.Value[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TenantRestrictedQueries_DoNotWidenToOrganizationRecords()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(_orgA, _tenantA, [UserRole.TenantViewer]));

        var runs = await _service.GetRunsAsync(_orgA, null, 100);
        var events = await _service.GetEventsAsync(_orgA, null, 100, null);

        Assert.True(runs.IsSuccess);
        Assert.Single(runs.Value);
        Assert.Equal(_tenantA, runs.Value[0].TenantId);
        Assert.True(events.IsSuccess);
        Assert.Single(events.Value);
        Assert.Equal(_tenantA, events.Value[0].TenantId);
    }

    [Fact]
    public async Task PlatformQueries_CanSeeCrossOrganizationTechnicalHistory()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.PlatformAdmin]));

        var runs = await _service.GetRunsAsync(null, null, 100);
        var events = await _service.GetEventsAsync(null, null, 100, null);

        Assert.True(runs.IsSuccess);
        Assert.Equal(4, runs.Value.Count);
        Assert.Contains(runs.Value, run => run.OrganizationId == _orgB);
        Assert.True(events.IsSuccess);
        Assert.Equal(4, events.Value.Count);
    }

    [Fact]
    public async Task PlatformPipelineQuery_ReturnsEveryOrganizationWithSafePipelineStatus()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.PlatformAdmin]));

        var result = await _service.GetPlatformPipelinesAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal([_orgA, _orgB], result.Value.Select(organization => organization.OrganizationId));
        Assert.Contains(result.Value.Single(organization => organization.OrganizationId == _orgA).Pipelines,
            pipeline => pipeline.TenantId == _tenantA && pipeline.TenantName == "Tenant A");
        Assert.DoesNotContain(result.Value.SelectMany(organization => organization.Pipelines),
            pipeline => pipeline.TenantId == _systemTenantA);
    }

    [Fact]
    public async Task InvalidTake_ReturnsValidationError()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Employee]));

        var result = await _service.GetRunsAsync(_orgA, null, 0);

        Assert.True(result.IsFailed);
        Assert.IsType<Adwais.Application.Common.Errors.ValidationError>(result.Errors.Single());
    }

    [Fact]
    public async Task EventProjection_HidesExceptionLikeMessages()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Employee]));
        _db.SystemEvents.Add(Event(
            _orgA,
            _tenantA,
            SystemEventAudience.Organization,
            "System.InvalidOperationException: provider failed at step one at Service.Execute()",
            "private details"));
        await _db.SaveChangesAsync();

        var result = await _service.GetEventsAsync(_orgA, null, 100, null);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, item => item.Message == "An operational event was recorded.");
        Assert.DoesNotContain(result.Value, item => item.Message.Contains("InvalidOperationException", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunDetails_EnforceScopeAndOnlyIncludeOwnedEvents()
    {
        _access.SetupGet(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Employee]));
        var runId = _db.PipelineRuns
            .Where(run => run.OrganizationId == _orgA && run.TenantId == _tenantA)
            .Select(run => run.Id)
            .First();
        var matchingEvent = Event(
            _orgA,
            _tenantA,
            SystemEventAudience.Organization,
            "Matching event",
            "safe details");
        matchingEvent.PipelineRunId = runId;
        _db.SystemEvents.Add(matchingEvent);
        var mismatchedEvent = Event(
            _orgB,
            _tenantB,
            SystemEventAudience.Organization,
            "Wrong organization",
            "wrong details");
        mismatchedEvent.PipelineRunId = runId;
        _db.SystemEvents.Add(mismatchedEvent);
        await _db.SaveChangesAsync();

        var details = await _service.GetRunAsync(_orgA, runId);
        var otherRun = await _service.GetRunAsync(_orgA,
            _db.PipelineRuns.Single(run => run.OrganizationId == _orgB).Id);

        Assert.True(details.IsSuccess);
        Assert.Single(details.Value.Events);
        Assert.Equal(_tenantA, details.Value.Events[0].TenantId);
        Assert.True(otherRun.IsFailed);
        Assert.IsType<Adwais.Application.Common.Errors.NotFoundError>(otherRun.Errors.Single());
    }

    private static PipelineRun Run(Guid organizationId, Guid? tenantId, PipelineRunState state, string summary)
        => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantId = tenantId,
            Kind = PipelineKind.OrderIngestion,
            Trigger = PipelineTriggerKind.Scheduled,
            RequestedAt = DateTimeOffset.UtcNow,
            LastStateChangedAt = DateTimeOffset.UtcNow,
            State = state,
            SafeSummary = summary
        };

    private static SystemEvent Event(
        Guid? organizationId,
        Guid? tenantId,
        SystemEventAudience audience,
        string message,
        string details)
        => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantId = tenantId,
            Audience = audience,
            Code = "test.event",
            Level = SystemEventLevel.Error,
            Source = "test",
            Message = message,
            Details = details
        };

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
