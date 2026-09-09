// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public sealed class DiagnosticsService(
    IApplicationDbContext db,
    ICurrentAccess currentAccess,
    ISystemHealthService healthService) : IDiagnosticsService
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromHours(24);

    public async Task<Result<OrganizationDiagnosticsDto>> GetOrganizationPipelinesAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var access = AuthorizeOrganization(organizationId);
        if (access.IsFailed) return Result.Fail<OrganizationDiagnosticsDto>(access.Errors);

        var organization = await db.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == organizationId, ct);
        if (organization is null)
            return Result.Fail<OrganizationDiagnosticsDto>(new NotFoundError("organization", organizationId));

        var filter = OrganizationFilter.From(currentAccess.Scope);
        var tenantsQuery = db.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.OrganizationId == organizationId && !tenant.IsSystem);
        if (filter.TenantId is { } tenantId)
            tenantsQuery = tenantsQuery.Where(tenant => tenant.Id == tenantId);
        var tenants = await tenantsQuery.ToListAsync(ct);

        // Feed sources are organization-owned and have no tenant grant. A
        // tenant-restricted principal therefore cannot see them.
        var feeds = filter.TenantId is null
            ? await db.FeedSources
                .AsNoTracking()
                .Where(feed => feed.OrganizationId == organizationId)
                .ToListAsync(ct)
            : [];

        var monitorsQuery = db.Monitors
            .AsNoTracking()
            .Include(monitor => monitor.Tenant)
            .Where(monitor => monitor.Tenant != null
                && monitor.Tenant.OrganizationId == organizationId
                && !monitor.Tenant.IsSystem);
        if (filter.TenantId is { } monitorTenantId)
            monitorsQuery = monitorsQuery.Where(monitor => monitor.TenantId == monitorTenantId);
        var monitors = await monitorsQuery.ToListAsync(ct);

        var organizationConfig = await db.OrganizationConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(config => config.OrganizationId == organizationId, ct);

        var runs = await db.PipelineRuns
            .AsNoTracking()
            .Where(run => run.OrganizationId == organizationId)
            .OrderByDescending(run => run.RequestedAt)
            .Take(500)
            .ToListAsync(ct);

        var observedAt = DateTimeOffset.UtcNow;
        var statuses = new List<PipelineStatusDto>(tenants.Count + feeds.Count + monitors.Count);
        statuses.AddRange(tenants.Select(tenant =>
        {
            var latest = LatestRun(runs, tenant.Id, PipelineKind.OrderIngestion, null);
            return BuildTenantStatus(
                tenant,
                latest,
                LatestSuccessfulRun(runs, tenant.Id, PipelineKind.OrderIngestion, null),
                organizationConfig?.OrderFetchIntervalMinutes ?? 60,
                organizationConfig?.OrderFetchEnabled ?? true,
                observedAt);
        }));
        statuses.AddRange(feeds.Select(feed => BuildFeedStatus(
            feed,
            LatestRun(runs, null, PipelineKind.FeedRefresh, feed.Id.ToString("D")),
            LatestSuccessfulRun(runs, null, PipelineKind.FeedRefresh, feed.Id.ToString("D")),
            organizationConfig?.FeedFetchIntervalHours ?? 2,
            observedAt)));
        statuses.AddRange(monitors.Select(monitor => BuildMonitorStatus(
            monitor,
            LatestRun(runs, monitor.TenantId, PipelineKind.MonitorSync, monitor.Id.ToString()),
            LatestSuccessfulRun(runs, monitor.TenantId, PipelineKind.MonitorSync, monitor.Id.ToString()),
            organizationConfig?.MonitoringFetchEnabled ?? true,
            observedAt)));

        var attention = statuses.Count(status => status.IssueCode is not null
            || status.Freshness is "Overdue" or "NeverSucceeded"
            || status.State is "Failed" or "RetryScheduled");
        var running = statuses.Count(status => status.State is "Pending" or "Queued" or "Running");
        var healthy = statuses.Count(status => status.IssueCode is null
            && status.Freshness is "Current" or "NotApplicable"
            && status.State is not "Pending" and not "Queued" and not "Running"
            && status.State is not "Failed" and not "RetryScheduled");

        return Result.Ok(new OrganizationDiagnosticsDto(
            organization.Id,
            organization.Name,
            observedAt,
            statuses.Count,
            Math.Max(0, healthy),
            attention,
            running,
            statuses));
    }

    public async Task<Result<IReadOnlyList<OrganizationDiagnosticsDto>>> GetPlatformPipelinesAsync(
        Guid? organizationId,
        CancellationToken ct = default)
    {
        if (currentAccess.Scope?.IsPlatformAdmin != true)
            return Result.Fail<IReadOnlyList<OrganizationDiagnosticsDto>>(
                new ScopeDeniedError("platform administration", "the current scope"));

        var organizationIds = organizationId is { } selectedOrganizationId
            ? new List<Guid> { selectedOrganizationId }
            : await db.Organizations
                .AsNoTracking()
                .OrderBy(organization => organization.Name)
                .Select(organization => organization.Id)
                .ToListAsync(ct);

        var diagnostics = new List<OrganizationDiagnosticsDto>(organizationIds.Count);
        foreach (var currentOrganizationId in organizationIds)
        {
            var result = await GetOrganizationPipelinesAsync(currentOrganizationId, ct);
            if (result.IsFailed)
                return Result.Fail<IReadOnlyList<OrganizationDiagnosticsDto>>(result.Errors);
            diagnostics.Add(result.Value);
        }

        return Result.Ok<IReadOnlyList<OrganizationDiagnosticsDto>>(diagnostics);
    }

    public async Task<Result<IReadOnlyList<PipelineRunDto>>> GetRunsAsync(
        Guid? organizationId,
        Guid? tenantId,
        int take,
        CancellationToken ct = default)
    {
        var validation = ValidateTake(take);
        if (validation is not null) return Result.Fail<IReadOnlyList<PipelineRunDto>>(validation);

        var authorization = await AuthorizeQueryOrganizationAsync(organizationId, ct);
        if (authorization.IsFailed) return Result.Fail<IReadOnlyList<PipelineRunDto>>(authorization.Errors);

        var tenantValidation = await ValidateTenantAsync(organizationId, tenantId, ct);
        if (tenantValidation.IsFailed) return Result.Fail<IReadOnlyList<PipelineRunDto>>(tenantValidation.Errors);

        var filter = OrganizationFilter.From(currentAccess.Scope);
        if (filter.TenantId is { } restrictedTenant
            && tenantId is { } requestedTenant
            && restrictedTenant != requestedTenant)
            return Result.Fail<IReadOnlyList<PipelineRunDto>>(ScopeDenied());

        IQueryable<PipelineRun> query = db.PipelineRuns
            .AsNoTracking()
            .Include(run => run.Tenant)
            .Include(run => run.Organization);
        if (organizationId is { } orgId)
            query = query.Where(run => run.OrganizationId == orgId);
        if (tenantId is { } selectedTenant)
            query = query.Where(run => run.TenantId == selectedTenant);
        if (filter.TenantId is { } visibleTenant)
            query = query.Where(run => run.TenantId == visibleTenant);
        if (currentAccess.Scope?.IsPlatformAdmin != true)
            query = query.Where(run => run.TenantId == null
                || !db.Tenants.Any(tenant => tenant.Id == run.TenantId && tenant.IsSystem));

        var rows = await query
            .OrderByDescending(run => run.RequestedAt)
            .ThenByDescending(run => run.Id)
            .Take(take)
            .ToListAsync(ct);

        return Result.Ok<IReadOnlyList<PipelineRunDto>>(rows.Select(ToDto).ToList());
    }

    public async Task<Result<PipelineRunDetailsDto>> GetRunAsync(
        Guid? organizationId,
        Guid runId,
        CancellationToken ct = default)
    {
        var authorization = await AuthorizeQueryOrganizationAsync(organizationId, ct);
        if (authorization.IsFailed)
            return Result.Fail<PipelineRunDetailsDto>(authorization.Errors);

        var filter = OrganizationFilter.From(currentAccess.Scope);
        var query = db.PipelineRuns
            .AsNoTracking()
            .Include(run => run.Tenant)
            .Include(run => run.Organization)
            .Where(run => run.Id == runId);

        if (organizationId is { } requestedOrganization)
            query = query.Where(run => run.OrganizationId == requestedOrganization);
        if (filter.TenantId is { } visibleTenant)
            query = query.Where(run => run.TenantId == visibleTenant);
        if (currentAccess.Scope?.IsPlatformAdmin != true)
            query = query.Where(run => run.TenantId == null
                || !db.Tenants.Any(tenant => tenant.Id == run.TenantId && tenant.IsSystem));

        var run = await query.SingleOrDefaultAsync(ct);
        if (run is null)
            return Result.Fail<PipelineRunDetailsDto>(new NotFoundError("pipeline run", runId));

        var eventsQuery = db.SystemEvents
            .AsNoTracking()
            .Include(systemEvent => systemEvent.Organization)
            .Include(systemEvent => systemEvent.Tenant)
            .Where(systemEvent => systemEvent.PipelineRunId == runId
                && systemEvent.OrganizationId == run.OrganizationId
                && systemEvent.TenantId == run.TenantId);
        if (currentAccess.Scope?.IsPlatformAdmin != true)
            eventsQuery = eventsQuery.Where(systemEvent => systemEvent.Audience != SystemEventAudience.Platform);

        var events = await eventsQuery
            .OrderByDescending(systemEvent => systemEvent.Timestamp)
            .ThenByDescending(systemEvent => systemEvent.Id)
            .Take(100)
            .ToListAsync(ct);

        return Result.Ok(new PipelineRunDetailsDto(ToDto(run), events.Select(ToDto).ToList()));
    }

    public async Task<Result<IReadOnlyList<DiagnosticEventDto>>> GetEventsAsync(
        Guid? organizationId,
        Guid? tenantId,
        int take,
        SystemEventLevel? minLevel,
        CancellationToken ct = default)
    {
        var validation = ValidateTake(take);
        if (validation is not null) return Result.Fail<IReadOnlyList<DiagnosticEventDto>>(validation);

        var authorization = await AuthorizeQueryOrganizationAsync(organizationId, ct);
        if (authorization.IsFailed) return Result.Fail<IReadOnlyList<DiagnosticEventDto>>(authorization.Errors);

        var tenantValidation = await ValidateTenantAsync(organizationId, tenantId, ct);
        if (tenantValidation.IsFailed) return Result.Fail<IReadOnlyList<DiagnosticEventDto>>(tenantValidation.Errors);

        var filter = OrganizationFilter.From(currentAccess.Scope);
        if (filter.TenantId is { } restrictedTenant
            && tenantId is { } requestedTenant
            && restrictedTenant != requestedTenant)
            return Result.Fail<IReadOnlyList<DiagnosticEventDto>>(ScopeDenied());

        IQueryable<SystemEvent> query = db.SystemEvents
            .AsNoTracking()
            .Include(systemEvent => systemEvent.Organization)
            .Include(systemEvent => systemEvent.Tenant);
        if (organizationId is { } orgId)
        {
            query = query.Where(systemEvent => systemEvent.OrganizationId == orgId);
            if (currentAccess.Scope?.IsPlatformAdmin != true)
                query = query.Where(systemEvent => systemEvent.Audience != SystemEventAudience.Platform);
        }
        else
        {
            query = query.Where(systemEvent => systemEvent.Audience == SystemEventAudience.Platform
                || systemEvent.OrganizationId != null);
        }

        if (tenantId is { } selectedTenant)
            query = query.Where(systemEvent => systemEvent.TenantId == selectedTenant);
        if (filter.TenantId is { } visibleTenant)
            query = query.Where(systemEvent => systemEvent.TenantId == visibleTenant);
        if (organizationId is not null && currentAccess.Scope?.IsPlatformAdmin != true)
            query = query.Where(systemEvent => systemEvent.TenantId == null
                || !db.Tenants.Any(tenant => tenant.Id == systemEvent.TenantId && tenant.IsSystem));
        if (minLevel is { } minimum)
            query = query.Where(systemEvent => systemEvent.Level >= minimum);

        var rows = await query
            .OrderByDescending(systemEvent => systemEvent.Timestamp)
            .ThenByDescending(systemEvent => systemEvent.Id)
            .Take(take)
            .ToListAsync(ct);

        return Result.Ok<IReadOnlyList<DiagnosticEventDto>>(rows.Select(ToDto).ToList());
    }

    public async Task<Result<PlatformDiagnosticsDto>> GetPlatformDiagnosticsAsync(CancellationToken ct = default)
    {
        if (currentAccess.Scope?.IsPlatformAdmin != true)
            return Result.Fail<PlatformDiagnosticsDto>(new ScopeDeniedError("platform administration", "the current scope"));

        var health = await healthService.GetHealthAsync(ct);
        var organizations = await db.Organizations.AsNoTracking().ToListAsync(ct);
        var cutoff = DateTimeOffset.UtcNow.Subtract(RecentWindow);

        var failedRuns = await db.PipelineRuns
            .AsNoTracking()
            .Where(run => run.State == PipelineRunState.Failed && run.LastStateChangedAt >= cutoff)
            .GroupBy(run => run.OrganizationId)
            .Select(group => new { OrganizationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.OrganizationId, row => row.Count, ct);
        var activeRuns = await db.PipelineRuns
            .AsNoTracking()
            .Where(run => run.State == PipelineRunState.Pending
                || run.State == PipelineRunState.Queued
                || run.State == PipelineRunState.Running)
            .GroupBy(run => run.OrganizationId)
            .Select(group => new { OrganizationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.OrganizationId, row => row.Count, ct);
        var recentEvents = await db.SystemEvents
            .AsNoTracking()
            .Where(systemEvent => systemEvent.OrganizationId != null && systemEvent.Timestamp >= cutoff)
            .GroupBy(systemEvent => systemEvent.OrganizationId!.Value)
            .Select(group => new { OrganizationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.OrganizationId, row => row.Count, ct);

        var affected = organizations
            .Select(organization => new OrganizationIssueSummaryDto(
                organization.Id,
                organization.Name,
                failedRuns.GetValueOrDefault(organization.Id),
                activeRuns.GetValueOrDefault(organization.Id),
                recentEvents.GetValueOrDefault(organization.Id)))
            .Where(summary => summary.FailedRuns > 0 || summary.ActiveRuns > 0 || summary.RecentEvents > 0)
            .OrderByDescending(summary => summary.FailedRuns)
            .ThenByDescending(summary => summary.RecentEvents)
            .ToList();

        return Result.Ok(new PlatformDiagnosticsDto(DateTimeOffset.UtcNow, health, affected));
    }

    private Result AuthorizeOrganization(Guid organizationId)
    {
        var scope = currentAccess.Scope;
        if (scope is null)
            return Result.Fail(new ScopeDeniedError("an organization scope", "none"));
        if (!scope.IsPlatformAdmin && scope.OrganizationId != organizationId)
            return Result.Fail(ScopeDenied());
        return Result.Ok();
    }

    private async Task<Result> AuthorizeQueryOrganizationAsync(Guid? organizationId, CancellationToken ct)
    {
        if (organizationId is null)
        {
            return currentAccess.Scope?.IsPlatformAdmin == true
                ? Result.Ok()
                : Result.Fail(new ScopeDeniedError("platform administration", "the current scope"));
        }

        var authorization = AuthorizeOrganization(organizationId.Value);
        if (authorization.IsFailed) return authorization;

        return await db.Organizations.AnyAsync(organization => organization.Id == organizationId.Value, ct)
            ? Result.Ok()
            : Result.Fail(new NotFoundError("organization", organizationId.Value));
    }

    private async Task<Result> ValidateTenantAsync(Guid? organizationId, Guid? tenantId, CancellationToken ct)
    {
        if (tenantId is null) return Result.Ok();

        var tenants = db.Tenants
            .AsNoTracking()
            .Where(candidate => candidate.Id == tenantId.Value);
        if (currentAccess.Scope?.IsPlatformAdmin != true)
            tenants = tenants.Where(candidate => !candidate.IsSystem);

        var tenant = await tenants
            .Select(candidate => new { candidate.Id, candidate.OrganizationId })
            .SingleOrDefaultAsync(ct);
        if (tenant is null)
            return Result.Fail(new NotFoundError("tenant", tenantId.Value));
        if (organizationId is { } requestedOrganization && tenant.OrganizationId != requestedOrganization)
            return Result.Fail(ScopeDenied());
        return Result.Ok();
    }

    private ScopeDeniedError ScopeDenied()
        => new("the requested organization or tenant", currentAccess.Scope?.OrganizationId?.ToString() ?? "none");

    private static ValidationError? ValidateTake(int take)
        => take is >= 1 and <= 100
            ? null
            : new ValidationError(new Dictionary<string, string[]>
            {
                ["take"] = ["Take must be between 1 and 100."]
            });

    private static PipelineRun? LatestRun(IEnumerable<PipelineRun> runs, Guid? tenantId, PipelineKind kind, string? resourceKey)
        => runs.FirstOrDefault(run => run.TenantId == tenantId
            && run.Kind == kind
            && (resourceKey is null || run.ResourceKey == resourceKey));

    private static PipelineRun? LatestSuccessfulRun(
        IEnumerable<PipelineRun> runs,
        Guid? tenantId,
        PipelineKind kind,
        string? resourceKey)
        => runs.FirstOrDefault(run => run.TenantId == tenantId
            && run.Kind == kind
            && run.State == PipelineRunState.Succeeded
            && (resourceKey is null || run.ResourceKey == resourceKey));

    private static PipelineStatusDto BuildTenantStatus(
        Tenant tenant,
        PipelineRun? run,
        PipelineRun? successfulRun,
        int scheduleMinutes,
        bool organizationEnabled,
        DateTimeOffset observedAt)
    {
        var enabled = tenant.OrderFetchingEnabled && organizationEnabled;
        var issueCode = !enabled
            ? null
            : string.IsNullOrWhiteSpace(tenant.OrderProviderSettings)
                ? "configuration.missing"
                : tenant.LastSyncError is not null || run?.State == PipelineRunState.Failed
                    ? "pipeline.failed"
                    : null;
        var issueSummary = issueCode switch
        {
            "configuration.missing" => "Order ingestion is not configured for this tenant.",
            "pipeline.failed" => "The latest order ingestion failed.",
            _ => null
        };
        var runState = !enabled
            ? "Disabled"
            : run?.State.ToString() ?? (tenant.CurrentlyFetching ? "Running" : "Idle");
        if (enabled && tenant.CurrentlyFetching) runState = "Running";
        var lastSuccess = successfulRun?.CompletedAt;
        var lastAttempt = tenant.LastPolled ?? run?.RequestedAt;
        var nextExpected = lastAttempt?.AddMinutes(Math.Max(1, scheduleMinutes));
        return new PipelineStatusDto(
            PipelineKind.OrderIngestion.ToString(),
            tenant.Id.ToString("D"),
            tenant.Name,
            "Order ingestion",
            tenant.Id,
            tenant.Name,
            enabled,
            !string.IsNullOrWhiteSpace(tenant.OrderProviderSettings),
            $"Every {Math.Max(1, scheduleMinutes)} minutes",
            nextExpected,
            runState,
            Freshness(lastSuccess, enabled, nextExpected, observedAt, lastAttempt),
            lastAttempt,
            lastSuccess,
            tenant.FetchedUntil,
            run?.State is PipelineRunState.Pending or PipelineRunState.Queued or PipelineRunState.Running ? run.Id : null,
            issueCode,
            issueSummary,
            issueCode == "configuration.missing" ? "Configure the order provider for this tenant." : issueCode is null ? null : "Review the failed run and retry after correcting the cause.");
    }

    private static PipelineStatusDto BuildFeedStatus(
        Adwais.Domain.Entities.Intranet.FeedSource feed,
        PipelineRun? run,
        PipelineRun? successfulRun,
        int scheduleHours,
        DateTimeOffset observedAt)
    {
        var issueCode = !feed.IsActive || (feed.LastSyncError is null && run?.State != PipelineRunState.Failed)
            ? null
            : "pipeline.failed";
        var lastAttempt = feed.LastPolledAt.HasValue
            ? new DateTimeOffset(feed.LastPolledAt.Value, TimeSpan.Zero)
            : run?.RequestedAt;
        var nextExpected = lastAttempt?.AddHours(Math.Max(1, scheduleHours));
        return new PipelineStatusDto(
            PipelineKind.FeedRefresh.ToString(),
            feed.Id.ToString("D"),
            feed.Name,
            "Feed refresh",
            null,
            null,
            feed.IsActive,
            !string.IsNullOrWhiteSpace(feed.Url),
            $"Every {Math.Max(1, scheduleHours)} hours",
            nextExpected,
            !feed.IsActive ? "Disabled" : run?.State.ToString() ?? (issueCode is null ? "Idle" : "Failed"),
            Freshness(successfulRun?.CompletedAt ?? (feed.LastSuccessAt.HasValue
                ? new DateTimeOffset(feed.LastSuccessAt.Value, TimeSpan.Zero)
                : null), feed.IsActive, nextExpected, observedAt, lastAttempt),
            lastAttempt,
            successfulRun?.CompletedAt ?? (feed.LastSuccessAt.HasValue
                ? new DateTimeOffset(feed.LastSuccessAt.Value, TimeSpan.Zero)
                : null),
            null,
            run?.State is PipelineRunState.Pending or PipelineRunState.Queued or PipelineRunState.Running ? run.Id : null,
            issueCode,
            issueCode is null ? null : "The latest feed refresh failed.",
            issueCode is null ? null : "Review the feed URL and retry the refresh.");
    }

    private static PipelineStatusDto BuildMonitorStatus(
        Adwais.Domain.Entities.Monitoring.UptimeMonitor monitor,
        PipelineRun? run,
        PipelineRun? successfulRun,
        bool organizationEnabled,
        DateTimeOffset observedAt)
    {
        var resourceLastSuccess = new[] { monitor.LastUpdate, monitor.LastUptimeUpdate, monitor.LastLatencyUpdate }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Cast<DateTimeOffset?>()
            .Max();
        var lastSuccess = successfulRun?.CompletedAt ?? resourceLastSuccess;
        var issueCode = !monitor.UptimeMonitorEnabled || !organizationEnabled
            ? null
            : monitor.LastSyncError is null && run?.State != PipelineRunState.Failed
            ? null
            : "pipeline.failed";
        var intervalSeconds = Math.Max(60, monitor.UpdateInterval);
        var enabled = monitor.UptimeMonitorEnabled && organizationEnabled;
        var nextExpected = lastSuccess?.AddSeconds(intervalSeconds);
        var lastAttempt = run?.LastStateChangedAt ?? resourceLastSuccess;
        return new PipelineStatusDto(
            PipelineKind.MonitorSync.ToString(),
            monitor.Id.ToString(),
            monitor.Name,
            "Monitor synchronization",
            monitor.TenantId,
            monitor.Tenant?.Name,
            enabled,
            !string.IsNullOrWhiteSpace(monitor.Url),
            $"Every {Math.Max(1, intervalSeconds / 60)} minutes",
            nextExpected,
            !enabled ? "Disabled" : run?.State.ToString() ?? (issueCode is null ? "Idle" : "Failed"),
            Freshness(lastSuccess, enabled, nextExpected, observedAt, lastAttempt),
            lastAttempt,
            lastSuccess,
            null,
            run?.State is PipelineRunState.Pending or PipelineRunState.Queued or PipelineRunState.Running ? run.Id : null,
            issueCode,
            issueCode is null ? null : "The latest monitor synchronization failed.",
            issueCode is null ? null : "Review the monitor configuration and retry synchronization.");
    }

    private static string Freshness(
        DateTimeOffset? lastSuccess,
        bool enabled,
        DateTimeOffset? nextExpected,
        DateTimeOffset observedAt,
        DateTimeOffset? lastAttempt)
    {
        if (!enabled) return "NotApplicable";
        if (!lastSuccess.HasValue) return lastAttempt.HasValue ? "NeverSucceeded" : "Unknown";
        return nextExpected.HasValue && observedAt > nextExpected.Value.AddMinutes(5)
            ? "Overdue"
            : "Current";
    }

    private static PipelineRunDto ToDto(PipelineRun run)
        => new(
            run.Id,
            run.OrganizationId,
            run.Organization?.Name,
            run.TenantId,
            run.Tenant?.Name,
            run.Kind.ToString(),
            run.Trigger.ToString(),
            run.State.ToString(),
            run.ResourceKey,
            run.ResourceName,
            run.RequestedAt,
            run.StartedAt,
            run.LastStateChangedAt,
            run.CompletedAt,
            run.NextRetryAt,
            run.AttemptCount,
            run.OutcomeCode,
            run.SafeSummary,
            run.WorkCount,
            run.RequestId,
            run.TraceId);

    private static DiagnosticEventDto ToDto(SystemEvent systemEvent)
        => new(
            systemEvent.Id,
            systemEvent.Timestamp,
            systemEvent.Level.ToString(),
            systemEvent.Code,
            systemEvent.Source,
            SafeEventMessage(systemEvent),
            systemEvent.SuggestedAction,
            systemEvent.OrganizationId,
            systemEvent.Organization?.Name,
            systemEvent.TenantId,
            systemEvent.Tenant?.Name,
            systemEvent.PipelineRunId,
            systemEvent.RequestId,
            systemEvent.TraceId);

    private static string SafeEventMessage(SystemEvent systemEvent)
    {
        var message = systemEvent.Code switch
        {
            "pipeline.failed" => systemEvent.TenantId is not null
                ? "A pipeline failed for this tenant."
                : systemEvent.OrganizationId is not null
                    ? "A pipeline failed for this organization."
                    : "A pipeline failed.",
            "pipeline.succeeded" => "A pipeline completed successfully.",
            "configuration.missing" => "Pipeline configuration is missing.",
            "provider.failure" => "A provider request failed.",
            "provider.timeout" => "A provider request timed out.",
            "pipeline.gap-too-large" => "Automated ingestion was skipped because the data gap is too large.",
            "pipeline.stale-state-reset" => "A stale pipeline state was reset.",
            _ => systemEvent.Message
        };

        var normalized = message.ReplaceLineEndings(" ").Trim();
        if (normalized.Contains(" at ", StringComparison.Ordinal)
            || normalized.Contains("Exception", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("StackTrace", StringComparison.OrdinalIgnoreCase))
            return "An operational event was recorded.";

        return normalized.Length <= 500 ? normalized : normalized[..500];
    }
}
