// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using Adwais.Application.Interfaces;
using Hangfire.Common;
using Hangfire.States;

namespace Adwais.Infrastructure.Jobs;

/// <summary>
/// Rejects enqueueing org-scoped jobs without a non-empty organization id
/// as their first argument. Enforced at state election time, so an invalid
/// payload never enters storage.
/// </summary>
public class OrgScopedJobEnforcementFilter : IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not EnqueuedState and not ScheduledState) return;

        var job = context.BackgroundJob.Job;
        if (job?.Type == null) return;

        EnsureOrgScope(job);
    }

    internal static void EnsureOrgScope(Job job)
    {
        if (!typeof(IOrgScopedJob).IsAssignableFrom(job.Type)) return;

        var orgId = ParseOrgId(job);
        if (orgId is null || orgId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Job {job.Type.Name} requires a non-empty organization id as its first argument.");
        }
    }

    private static Guid? ParseOrgId(Job job)
    {
        if (job.Args is null || job.Args.Count == 0) return null;

        return job.Args[0] switch
        {
            Guid guid => guid,
            string str when Guid.TryParse(str, out var parsed) => parsed,
            _ => null
        };
    }
}