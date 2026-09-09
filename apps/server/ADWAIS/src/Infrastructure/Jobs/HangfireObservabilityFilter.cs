// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Adwais.Application.Common.Observability;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

namespace Adwais.Infrastructure.Jobs;

/// <summary>
/// Carries W3C trace context from an enqueueing request into the Hangfire
/// server activity. Telemetry is best effort and never changes job behavior.
/// </summary>
public sealed class HangfireObservabilityFilter : JobFilterAttribute, IClientFilter, IServerFilter
{
    private const string TraceParentParameter = "adwais.traceparent";
    private const string TraceStateParameter = "adwais.tracestate";
    private const string ActivityItem = "adwais.hangfire.activity";

    public void OnCreating(CreatingContext filterContext)
    {
        try
        {
            var activity = Activity.Current;
            if (activity is null) return;

            filterContext.SetJobParameter(TraceParentParameter, activity.Id);
            if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
                filterContext.SetJobParameter(TraceStateParameter, activity.TraceStateString);
        }
        catch
        {
            // An exporter or storage problem must never prevent a job from
            // being enqueued.
        }
    }

    public void OnCreated(CreatedContext filterContext)
    {
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        try
        {
            var traceParent = filterContext.GetJobParameter<string>(TraceParentParameter);
            var traceState = filterContext.GetJobParameter<string>(TraceStateParameter);
            var activity = ActivityContext.TryParse(traceParent, traceState, out var parent)
                ? ObservabilityTelemetry.ActivitySource.StartActivity(
                    "adwais.hangfire.execute", ActivityKind.Consumer, parent)
                : ObservabilityTelemetry.ActivitySource.StartActivity(
                    "adwais.hangfire.execute", ActivityKind.Consumer);

            activity?.SetTag("adwais.hangfire.job_id", filterContext.BackgroundJob?.Id);
            activity?.SetTag("adwais.hangfire.job_type", filterContext.BackgroundJob?.Job?.Type?.FullName);
            filterContext.Items[ActivityItem] = activity;
        }
        catch
        {
            // Observability must not turn a valid Hangfire job into a failed
            // job when tracing metadata is unavailable.
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (!filterContext.Items.TryGetValue(ActivityItem, out var value)
            || value is not Activity activity)
            return;

        try
        {
            activity.Stop();
        }
        catch
        {
            // Activity shutdown is best effort.
        }
    }
}
