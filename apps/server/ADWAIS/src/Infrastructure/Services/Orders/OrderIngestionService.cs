// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Adwais.Application.Common.Exceptions;
using Adwais.Application.Common.Observability;
using Adwais.Application.DTOs.Financial.Upstream;
using Adwais.Domain;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

using Adwais.Application.Interfaces;

namespace Adwais.Infrastructure.Services;

public class OrderIngestionService(
    IDbContextFactory<AnalyticsDbContext> contextFactory,
    IEnumerable<IOrderSource> orderSources,
    ILogger<OrderIngestionService> logger,
    ISystemEventService eventService,
    IViewRefreshTracker viewRefreshTracker,
    IPipelineRunService? pipelineRunService = null)
    : IOrderIngestionService
{
    public async Task<int> ExecuteIngestionAsync(Guid organizationId, Guid tenantId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default)
        => await ExecuteIngestionInternalAsync(organizationId, tenantId, startDate, endDate, null, ct);

    public async Task<int> ExecuteIngestionTrackedAsync(Guid organizationId, Guid tenantId, DateTimeOffset startDate, DateTimeOffset endDate, Guid pipelineRunId, CancellationToken ct = default)
        => await ExecuteIngestionInternalAsync(organizationId, tenantId, startDate, endDate, pipelineRunId, ct);

    private async Task<int> ExecuteIngestionInternalAsync(Guid organizationId, Guid tenantId, DateTimeOffset startDate, DateTimeOffset endDate, Guid? pipelineRunId, CancellationToken ct)
    {
        Tenant tenant;
        await using (var context = await contextFactory.CreateDbContextAsync(ct))
        {
            tenant = await context.Tenants.FirstAsync(t => t.Id == tenantId, ct);
        }

        if (tenant.OrganizationId != organizationId)
            throw new InvalidOperationException($"Tenant {tenantId} does not belong to organization {organizationId}.");

        var hadPreviousError = tenant.LastSyncError is not null;
        var startedAt = Stopwatch.GetTimestamp();
        var run = pipelineRunService is null
            ? null
            : pipelineRunId is { } trackedRunId
                ? await LoadRunAsync(trackedRunId, tenant, ct)
                : await pipelineRunService.StartAsync(
                organizationId,
                tenantId,
                PipelineKind.OrderIngestion,
                PipelineTriggerKind.System,
                resourceKey: tenantId.ToString("D"),
                resourceName: tenant.Name,
                traceId: Activity.Current?.TraceId.ToHexString(),
                ct: ct);
        if (run is not null)
            await pipelineRunService!.MarkRunningAsync(run.Id, ct);

        using var activity = ObservabilityTelemetry.ActivitySource.StartActivity(
            "adwais.pipeline.order_ingestion", ActivityKind.Internal);
        activity?.SetTag("adwais.organization_id", organizationId);
        activity?.SetTag("adwais.tenant_id", tenantId);
        activity?.SetTag("adwais.pipeline", PipelineKind.OrderIngestion.ToString());
        activity?.SetTag("adwais.pipeline_run_id", run?.Id);
        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OrganizationId"] = organizationId,
            ["TenantId"] = tenantId,
            ["Pipeline"] = PipelineKind.OrderIngestion.ToString(),
            ["PipelineRunId"] = run?.Id,
            ["TraceId"] = Activity.Current?.TraceId.ToHexString()
        });
        ObservabilityTelemetry.PipelineAttempts.Add(1,
            new KeyValuePair<string, object?>("pipeline", "order_ingestion"));

        try
        {
            var result = await ExecuteIngestionCoreAsync(tenant, orderSources.ForProvider(tenant.OrderProvider), startDate, endDate, ct);
            
            await using (var context = await contextFactory.CreateDbContextAsync(ct))
            {
                var t = await context.Tenants.FirstAsync(x => x.Id == tenantId, ct);
                t.LastSyncError = null;
                await context.SaveChangesAsync(ct);
            }

            if (result > 0 || hadPreviousError)
            {
                await eventService.LogAsync(
                    nameof(OrderIngestionService),
                    hadPreviousError
                        ? "Order ingestion recovered for this tenant."
                        : $"Successfully ingested {result} orders.",
                    SystemEventLevel.Information,
                    details: null,
                    tenantId: tenantId,
                    organizationId: organizationId,
                    code: "pipeline.succeeded",
                    audience: SystemEventAudience.Organization,
                    pipelineRunId: run?.Id,
                    traceId: Activity.Current?.TraceId.ToHexString());
                await viewRefreshTracker.MarkDirtyAsync(tenant.OrganizationId, ct);
            }

            if (run is not null)
                await pipelineRunService!.CompleteAsync(run.Id, result, $"Ingestion completed. {result} orders committed.", ct);
            ObservabilityTelemetry.PipelineOutcomes.Add(1,
                new KeyValuePair<string, object?>("pipeline", "order_ingestion"),
                new KeyValuePair<string, object?>("outcome", "succeeded"));
            ObservabilityTelemetry.PipelineDuration.Record(
                Stopwatch.GetElapsedTime(startedAt).TotalSeconds,
                new KeyValuePair<string, object?>("pipeline", "order_ingestion"));
            
            return result;
        }
        catch (Exception ex)
        {
            var step = ex.Data.Contains("Step") ? ex.Data["Step"]?.ToString() : "Executing Ingestion Core";
            var detailedErrorMessage = $"Failed during step '{step}': {ex.Message}";
            var outcomeCode = ex is ConfigurationException ? "configuration.missing" : "provider.failure";
            var safeMessage = ex is ConfigurationException
                ? "Order ingestion is not configured for this tenant."
                : "The order provider failed while ingestion was running.";

            logger.LogError(ex,
                "Order ingestion failed for organization {OrganizationId}, tenant {TenantId}, run {RunId}.",
                organizationId, tenantId, run?.Id);
            await eventService.LogErrorAsync(
                nameof(OrderIngestionService),
                safeMessage,
                ex,
                tenantId: tenantId,
                organizationId: organizationId,
                code: outcomeCode,
                audience: SystemEventAudience.Organization,
                pipelineRunId: run?.Id,
                traceId: Activity.Current?.TraceId.ToHexString(),
                suggestedAction: ex is ConfigurationException
                    ? "Configure the order provider before retrying."
                    : "Review the provider configuration and retry the ingestion.");
            if (run is not null)
                await pipelineRunService!.FailAsync(run.Id, outcomeCode, safeMessage, CancellationToken.None);
            ObservabilityTelemetry.PipelineOutcomes.Add(1,
                new KeyValuePair<string, object?>("pipeline", "order_ingestion"),
                new KeyValuePair<string, object?>("outcome", "failed"),
                new KeyValuePair<string, object?>("error_code", outcomeCode));
            ObservabilityTelemetry.PipelineDuration.Record(
                Stopwatch.GetElapsedTime(startedAt).TotalSeconds,
                new KeyValuePair<string, object?>("pipeline", "order_ingestion"));
            
            try
            {
                await using var errorContext = await contextFactory.CreateDbContextAsync(CancellationToken.None);
                var t = await errorContext.Tenants.FirstAsync(x => x.Id == tenantId, CancellationToken.None);
                t.LastSyncError = detailedErrorMessage;
                await errorContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Failed to record error in Tenant {TenantId}.", tenantId);
            }

            throw;
        }
        finally
        {
            try
            {
                await using var cleanupContext = await contextFactory.CreateDbContextAsync(CancellationToken.None);
                var t = await cleanupContext.Tenants.FirstAsync(x => x.Id == tenantId, cancellationToken: CancellationToken.None);
                t.CurrentlyFetching = false;
                t.LastPolled = DateTimeOffset.UtcNow;
                await cleanupContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clear CurrentlyFetching flag for tenant {TenantId}.", tenantId);
            }
        }
    }

    private async Task<PipelineRun?> LoadRunAsync(Guid runId, Tenant tenant, CancellationToken ct)
    {
        // The run id is created by an authorized API or dispatcher. If it is
        // missing (for example after a partial enqueue), start a replacement
        // record so execution still has an observable owner.
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var existing = await db.PipelineRuns.SingleOrDefaultAsync(run => run.Id == runId, ct);
        return existing ?? await pipelineRunService!.StartAsync(
            tenant.OrganizationId,
            tenant.Id,
            PipelineKind.OrderIngestion,
            PipelineTriggerKind.System,
            resourceKey: tenant.Id.ToString("D"),
            resourceName: tenant.Name,
            ct: ct);
    }

    private async Task<int> ExecuteIngestionCoreAsync(Tenant tenant, IOrderSource orderSource, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct)
    {
        if (!orderSource.IsConfigured(tenant.OrderProviderSettings))
            throw new ConfigurationException("Order provider settings are missing or invalid.");
        var totalIngested = 0;
        var currentStart = startDate;

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        while (currentStart < endDate)
        {
            var currentEnd = currentStart.AddDays(30);
            if (currentEnd > endDate) currentEnd = endDate;

            var chunkStart = currentStart;
            var take = 500;
            var hasMoreOrders = true;

            while (hasMoreOrders)
            {
                IReadOnlyList<OrderSourceOrder> orders;
                try
                {
                    orders = await orderSource.FetchOrdersAsync(tenant.OrderProviderSettings!, currentStart, currentEnd, take, ct);
                    ObservabilityTelemetry.ProviderOutcomes.Add(1,
                        new KeyValuePair<string, object?>("provider", orderSource.Provider),
                        new KeyValuePair<string, object?>("outcome", "succeeded"));
                }
                catch (HttpRequestException ex)
                {
                    ObservabilityTelemetry.ProviderOutcomes.Add(1,
                        new KeyValuePair<string, object?>("provider", orderSource.Provider),
                        new KeyValuePair<string, object?>("outcome", "failed"));
                    logger.LogError(ex, "Failed to fetch chunk {Start} to {End} for Tenant {TenantId}.", currentStart, currentEnd, tenant.Id);
                    throw;
                }

                if (orders.Count != 0)
                {
                    var count = orders.Count;
                    var pIds = new Guid[count];
                    var pTenantIds = new Guid[count];
                    var pProviders = new string[count];
                    var pExternalIds = new string[count];
                    var pOrderStatus = new string[count];
                    var pOrderIds = new string[count];
                    var pDatesCreated = new DateTimeOffset[count];
                    var pIncVat = new decimal?[count];
                    var pExcVat = new decimal?[count];
                    var pCurrencies = new string[count];

                    for (int i = 0; i < count; i++)
                    {
                        var o = orders[i];
                        pIds[i] = Guid.NewGuid();
                        pTenantIds[i] = tenant.Id;
                        pProviders[i] = orderSource.Provider;
                        pExternalIds[i] = o.ExternalId;
                        pOrderStatus[i] = o.State.ToString();
                        pOrderIds[i] = o.OrderNumber;
                        pDatesCreated[i] = o.CreatedDate;
                        pIncVat[i] = o.TotalValueIncludingVat;
                        pExcVat[i] = o.TotalValueExcludingVat;
                        pCurrencies[i] = o.Currency;
                    }

                    await UpsertOrdersAsync(dbContext, pIds, pTenantIds, pProviders, pExternalIds, pOrderStatus, pOrderIds, pDatesCreated, pIncVat, pExcVat, pCurrencies);
                    totalIngested += count;

                    // Advance cursor to the exact timestamp of the final order in this payload
                    var nextCursor = orders[^1].CreatedDate;
                    currentStart = nextCursor == currentStart ? currentStart.AddTicks(1) : nextCursor;
                }

                hasMoreOrders = orders.Count == take;
            }

            var t = await dbContext.Tenants.SingleAsync(x => x.Id == tenant.Id, cancellationToken: ct);
            t.FetchedFrom = t.FetchedFrom == null ? chunkStart : (chunkStart < t.FetchedFrom ? chunkStart : t.FetchedFrom);                                                                                  
            t.FetchedUntil = t.FetchedUntil == null ? currentEnd : (currentEnd > t.FetchedUntil ? currentEnd : t.FetchedUntil);                                                                                    
            await dbContext.SaveChangesAsync(ct);      

            currentStart = currentEnd;
        }

        return totalIngested;
    }

    public async Task IngestSingleOrderAsync(Guid organizationId, Guid tenantId, string provider, OrderSourceOrder order, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var tenant = await dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.OrderProvider, t.IsSystem, t.OrganizationId })
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Tenant {tenantId} not found.");
        if (tenant.IsSystem)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} is not an order target.");
        }
        if (tenant.OrganizationId != organizationId)
            throw new InvalidOperationException($"Tenant {tenantId} does not belong to organization {organizationId}.");
        var orderSource = orderSources.ForProvider(provider);
        if (!tenant.OrderProvider.Equals(orderSource.Provider, StringComparison.OrdinalIgnoreCase))
            throw new ConfigurationException($"Tenant is configured for order provider '{tenant.OrderProvider}', not '{orderSource.Provider}'.");

        var pIds = new[] { Guid.NewGuid() };
        var pTenantIds = new[] { tenantId };
        var pProviders = new[] { orderSource.Provider };
        var pExternalIds = new[] { order.ExternalId };
        var pOrderStatus = new[] { order.State.ToString() };
        var pOrderIds = new[] { order.OrderNumber };
        var pDatesCreated = new[] { order.CreatedDate };
        var pIncVat = new[] { order.TotalValueIncludingVat };
        var pExcVat = new[] { order.TotalValueExcludingVat };
        var pCurrencies = new[] { order.Currency };

        await UpsertOrdersAsync(dbContext, pIds, pTenantIds, pProviders, pExternalIds, pOrderStatus, pOrderIds, pDatesCreated, pIncVat, pExcVat, pCurrencies);
        await viewRefreshTracker.MarkDirtyAsync(tenant.OrganizationId, ct);
    }

    private static async Task UpsertOrdersAsync(
        AnalyticsDbContext dbContext,
        Guid[] ids, Guid[] tenantIds, string[] providers, string[] externalIds, string[] orderStatuses,
        string[] orderNumbers, DateTimeOffset[] datesCreated,
        decimal?[] incVats, decimal?[] excVats, string[] currencies)
    {
        if (!dbContext.Database.IsRelational()) return;

        const string sql = @"
            INSERT INTO orders (id, tenant_id, provider, external_id, order_state, order_number, created_date, total_value_inc_vat, total_value_exc_vat, currency)
            SELECT * FROM UNNEST(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)
            ON CONFLICT (tenant_id, provider, external_id)
            DO UPDATE SET 
                total_value_inc_vat = EXCLUDED.total_value_inc_vat,
                total_value_exc_vat = EXCLUDED.total_value_exc_vat,
                order_state = EXCLUDED.order_state";

        await dbContext.Database.ExecuteSqlRawAsync(sql, ids, tenantIds, providers, externalIds, orderStatuses, orderNumbers, datesCreated, incVats, excVats, currencies);
    }
}

