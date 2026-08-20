// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {Play, Activity, Lock, Clock} from 'lucide-react';
import { useRecurringJobsQuery, useTriggerJobMutation, useBackfillMutation, useRecentJobsQuery } from '../../hooks/useJobSettingsQueries';
import { useTenantsQuery } from '../../hooks/useTenantQueries';
import { RecurringJobsTable } from '../../components/settings/jobs/RecurringJobsTable';
import { ManualBackfillPanel } from '../../components/settings/jobs/ManualBackfillPanel';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { ConsolePanel } from '../../components/common/layout/ConsolePanel';
import { ConsoleLoadingRows } from '../../components/common/ui/ConsoleLoadingRows';
import { useCurrentUser, type UserProfile } from '../../hooks/useCurrentUser';
import { formatDateTime } from '../../utils/dateTime';

type ManualJobAccess = 'admin' | 'staff';

interface ManualJob {
    id: string;
    name: string;
    desc: string;
    access: ManualJobAccess;
    url: string;
}

const manualJobs = [
    { id: 'monitor-sync', name: 'Monitor Sync', desc: 'Syncs monitor states from external providers.', access: 'admin', url: '/api/job/trigger/monitor-sync' },
    { id: 'uptime-sync', name: 'Uptime Sync', desc: 'Fetches latest uptime ping data.', access: 'admin', url: '/api/job/trigger/uptime-sync' },
    { id: 'latency-sync', name: 'Latency Sync', desc: 'Fetches latency metrics for all monitors.', access: 'admin', url: '/api/job/trigger/latency-sync' },
    { id: 'user-stats-sync', name: 'UptimeRobot User Stats', desc: 'Calculates active UptimeRobot user statistics.', access: 'admin', url: '/api/job/trigger/user-stats-sync' },
    { id: 'order-sync', name: 'Order Sync', desc: 'Synchronizes order data from external providers.', access: 'admin', url: '/api/job/trigger/order-sync' },
    { id: 'feed-fetch', name: 'Feed Fetch', desc: 'Triggers aggregation of RSS, blogs, and newsrooms immediately.', access: 'admin', url: '/api/global-config/feeds/fetch' },
    { id: 'refresh-historic-order-data', name: 'Refresh Historic Orders', desc: 'Rebuilds materialized views for old orders.', access: 'staff', url: '/api/job/trigger/refresh-historic-order-data' },
    { id: 'refresh-monitoring-data', name: 'Refresh Monitoring', desc: 'Rebuilds monitoring materialized views.', access: 'staff', url: '/api/job/trigger/refresh-monitoring-data' },
] satisfies readonly ManualJob[];

function canTriggerManualJob(role: UserProfile['role'] | null, access: ManualJobAccess) {
    return role === 'Admin' || (role === 'Employee' && access === 'staff');
}

export function BackgroundJobsView() {
    const recurringQuery = useRecurringJobsQuery();
    const tenantsQuery = useTenantsQuery();
    const recentJobsQuery = useRecentJobsQuery();
    const { data: recurring } = recurringQuery;
    const { data: tenants } = tenantsQuery;
    const { data: recentJobs } = recentJobsQuery;
    const { role } = useCurrentUser();

    const triggerJob = useTriggerJobMutation();
    const triggerBackfill = useBackfillMutation();

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-2 portrait-contained:grid-rows-2 gap-4 contained:h-full contained:min-h-0 pb-3">
            {/* Top Action Section / Left Pane */}
            <div className="flex min-h-0 flex-col gap-4 h-full">
                <SettingsPanel
                    title="Manual Triggers & Configuration"
                    subtitle="Run synchronization and maintenance tasks on demand."
                    icon={<Activity size={24} />}
                >
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                            {manualJobs.map(job => {
                                const isRestricted = !canTriggerManualJob(role, job.access);
                                return (
                                    <button
                                        key={job.id}
                                        onClick={() => !isRestricted && triggerJob.mutate(job.url)}
                                        disabled={isRestricted}
                                        className={`group relative flex min-h-14 flex-col overflow-hidden rounded-xl px-4 py-3 text-left transition-colors ${isRestricted
                                            ? 'cursor-not-allowed bg-on-surface/[0.12] text-on-surface/[0.38]'
                                            : 'cursor-pointer bg-primary-container text-on-surface hover:m3-elevation-1 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary'
                                            }`}
                                    >
                                        <div className="flex items-center gap-3">
                                            {isRestricted ? (
                                                <Lock size={14} className="text-on-surface/[0.38]" />
                                            ) : (
                                                <Play size={18} className="text-on-primary-container transition-transform group-hover:translate-x-0.5" />
                                            )}
                                            <span className="text-base h-[calc(1.5em*2)] flex items-center font-black">{job.name}</span>
                                            {isRestricted && (
                                                <span className="ml-auto rounded-full bg-surface px-2.5 py-1 text-sm font-bold text-on-surface-variant">
                                                    {job.access === 'admin' ? 'Admin' : 'Staff'}
                                                </span>
                                            )}
                                        </div>
                                        <span className="text-sm font-medium line-clamp-2 h-[calc(1.5em*2)] leading-5 text-on-surface-variant">{job.desc}</span>
                                    </button>
                                );
                            })}
                        </div>
                </SettingsPanel>

                <ManualBackfillPanel
                    tenants={tenants}
                    isLoading={tenantsQuery.isLoading}
                    isError={tenantsQuery.isError}
                    triggerBackfill={triggerBackfill}
                    disabled={role !== 'Admin'}
                />
            </div>

            {/* Scheduled Jobs Section & Console Panel / Right Pane */}
            <div className="flex min-h-0 flex-col gap-4 h-full">
                <SettingsPanel 
                    title="Scheduled Jobs"
                    subtitle="Recurring system schedules."
                    icon={<Clock size={24} />}
                    contentClassName="flex-1 min-h-0 overflow-hidden"
                >
                        <RecurringJobsTable
                            recurring={recurring}
                            isLoading={recurringQuery.isLoading}
                            isError={recurringQuery.isError}
                        />
                </SettingsPanel>

                {/* Recent Jobs */}
                <ConsolePanel
                    title="Recent executions"
                    subtitle="Newest dispatches first"
                    icon={<Activity size={18} />}
                    className="flex-1 min-h-[400px] landscape-contained:max-h-none max-h-[500px]"
                >
                    {recentJobsQuery.isLoading ? (
                        <ConsoleLoadingRows label="Loading recent executions" />
                    ) : recentJobsQuery.isError ? (
                        <div role="alert" className="flex h-full items-center justify-center p-4 text-center text-console-text">
                            Unable to load recent executions.
                        </div>
                    ) : recentJobs && recentJobs.length > 0 ? (
                        <div className="flex flex-col divide-y divide-console-border min-w-0">
                            {recentJobs.map((job) => {
                                const isProcessing = job.state === 'Processing';
                                const isSucceeded = job.state === 'Succeeded';
                                const isFailed = job.state === 'Failed';
                                return (
                                    <div key={job.jobId} className="group relative flex flex-col px-3 py-1 text-on-surface-variant transition-colors hover:bg-console-hover min-w-0">
                                                <div className="flex items-center justify-between gap-8 min-w-0">
                                                    <div className="flex items-center gap-6 min-w-0">
                                                        <span className="shrink-0 font-mono text-sm ">
                                                            {(() => {
                                                                if (!job.createdAt) return 'N/A';
                                                                const d = new Date(job.createdAt);
                                                                const year = d.getFullYear();
                                                                const month = String(d.getMonth() + 1).padStart(2, '0');
                                                                const day = String(d.getDate()).padStart(2, '0');
                                                                const time = formatDateTime(d, { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
                                                                return `${year}-${month}-${day} ${time}`;
                                                            })()}
                                                        </span>
                                                        <div className="flex items-center gap-4 min-w-0">
                                                            {isProcessing && (
                                                                <span className="flex h-2 w-2 relative shrink-0">
                                                                    <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-console-blue opacity-75"></span>
                                                                    <span className="relative inline-flex h-2 w-2 rounded-full bg-console-blue"></span>
                                                                </span>
                                                            )}
                                                            {isSucceeded && <div className="h-2 w-2 shrink-0 rounded-full bg-console-green"></div>}
                                                            {isFailed && <div className="h-2 w-2 shrink-0 rounded-full bg-console-red"></div>}
                                                            <span
                                                                className="truncate font-bold text-console-text"
                                                                title={job.jobName || ''}
                                                            >
                                                                {job.jobName}
                                                            </span>
                                                        </div>
                                                    </div>
                                                    <div className="flex items-center gap-6 text-sm font-bold shrink-0">
                                                        {job.durationSeconds !== null && (
                                                            <span className="font-mono font-normal ">
                                                                {job.durationSeconds.toFixed(1)}s
                                                            </span>
                                                        )}
                                                        {isProcessing && <span className="uppercase tracking-wide text-console-blue">Active</span>}
                                                        {isSucceeded && <span className="uppercase tracking-wide text-console-green">Success</span>}
                                                        {isFailed && <span className="uppercase tracking-wide text-console-red">Failed</span>}
                                                    </div>
                                                </div>
                                                {job.jobArgs && (
                                                    <div className="break-all pr-4 text-sm font-normal leading-relaxed text-on-surface-variant select-text">
                                                        ({job.jobArgs})
                                                    </div>
                                                )}
                                                <div className="flex flex-wrap items-center gap-x-8 gap-y-2 text-sm font-medium text-on-surface-variant">
                                                    <span>ID: {job.jobId}</span>
                                                    {job.tenantName && (
                                                        <span className="text-console-icon">Tenant: <strong className="">{job.tenantName}</strong></span>
                                                    )}
                                                    {job.monitorName && (
                                                        <span className="text-console-icon">Monitor: <strong className="">{job.monitorName}</strong></span>
                                                    )}
                                                </div>
                                                {isFailed && job.exceptionMessage && (
                                                    <div className="custom-scrollbar mt-1.5 overflow-x-auto rounded border border-console-red/40 bg-console-error-bg p-2 text-sm text-console-error-text select-text min-w-0">
                                                        <pre className="whitespace-pre-wrap break-all">{job.exceptionMessage}</pre>
                                                    </div>
                                                )}
                                            </div>
                                        );
                                    })}
                                </div>
                            ) : (
                                <div className="flex h-full items-center justify-center text-sm font-semibold tracking-wide text-on-surface-variant">
                                    No recent background jobs found
                                </div>
                            )}
                        </ConsolePanel>
            </div>
        </div>
    );
}
