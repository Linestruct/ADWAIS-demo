// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { HeartPulse, TerminalSquare, AlertCircle, CheckCircle2, AlertTriangle, Info, Check, Copy } from 'lucide-react';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { ConsolePanel } from '../../components/common/layout/ConsolePanel';
import { SecureButton } from '../../components/common/ui/SecureButton';
import { Skeleton } from '../../components/common/ui/Skeleton';
import { ConsoleLoadingRows } from '../../components/common/ui/ConsoleLoadingRows';
import { useSystemEventsViewModel, type SystemEvent } from '../../hooks/useSystemEventsViewModel';
import { formatDateTime } from '../../utils/dateTime';
import { usePostApiDashboardSession } from '../../api/generated/endpoints';

function timeAgo(date: string | number | null | undefined): string {
    if (!date) return 'Never';
    const seconds = Math.floor((new Date().getTime() - new Date(date).getTime()) / 1000);
    if (seconds < 5) return 'Just now';
    if (seconds < 60) return `${seconds}s ago`;
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    return `${Math.floor(hours / 24)}d ago`;
}

interface HealthStatusCardProps {
    title: string;
    subtitle: string;
    status: 'Healthy' | 'Degraded' | 'Failed' | 'Warning' | string;
    children?: React.ReactNode;
}

function HealthStatusCard({ title, subtitle, status, children }: HealthStatusCardProps) {
    const isHealthy = status === 'Healthy' || status === 'OK';
    const isWarning = status === 'Degraded' || status === 'Warning';
    const isFailed = status === 'Failed' || status === 'Error' || status === 'Critical';

    return (
        <article className="flex flex-col rounded-xl bg-surface-container p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-col">
                    <span className="text-sm font-bold text-on-surface">{title}</span>
                    <span className="text-sm text-on-surface-variant">{subtitle}</span>
                </div>
                {isHealthy && (
                    <span className="flex items-center gap-2 rounded-full bg-success-container px-3 py-1 text-sm font-bold text-on-success-container">
                        <CheckCircle2 size={16} /> <span>OK</span>
                    </span>
                )}
                {isWarning && (
                    <span className="flex items-center gap-2 rounded-full bg-warning-container px-3 py-1 text-sm font-bold text-on-warning-container">
                        <AlertTriangle size={16} /> <span>WARN</span>
                    </span>
                )}
                {isFailed && (
                    <span className="flex items-center gap-2 rounded-full bg-error-container px-3 py-1 text-sm font-bold text-on-error-container">
                        <AlertCircle size={16} /> <span>ERR</span>
                    </span>
                )}
            </div>
            {children}
        </article>
    );
}

export function SystemEventsView() {
    const {
        isAdmin,
        health,
        events,
        isLoadingHealth,
        isHealthError,
        isLoadingEvents,
        isEventsError,
        clearErrorsMutation
    } = useSystemEventsViewModel();
    const dashboardSessionMutation = usePostApiDashboardSession();

    const openDashboard = async () => {
        const dashboardTab = window.open('about:blank', '_blank');
        if (dashboardTab) dashboardTab.opener = null;

        try {
            await dashboardSessionMutation.mutateAsync();
            if (dashboardTab) {
                dashboardTab.location.replace('/hangfire');
            } else {
                window.location.assign('/hangfire');
            }
        } catch (error) {
            dashboardTab?.close();
            window.alert(error instanceof Error ? error.message : 'Unable to open the Hangfire dashboard.');
        }
    };

    return (
        <div className="grid landscape-contained:grid-cols-2 portrait-contained:grid-rows-2 gap-4 contained:h-full contained:min-h-0">

            {/* Diagnostics / Health Panel */}
            <SettingsPanel className="">
                <SettingsPanelHeader
                    title="Pipeline Health"
                    subtitle="Live connectivity and ingestion status."
                    icon={<HeartPulse size={24} />}
                />

                <div className="custom-scrollbar flex flex-1 flex-col gap-6 overflow-y-auto p-3 sm:p-4">
                    {health ? (
                        <div className="flex shrink-0 flex-col gap-4">

                            {/* Database Health Card */}
                            <HealthStatusCard
                                title="Database Status"
                                subtitle="Core database connection"
                                status={health.databaseStatus}
                            />

                            {/* Sync Pipeline Health Card */}
                            <HealthStatusCard
                                title="Sync Status"
                                subtitle="Monitoring & order ingestion"
                                status={health.sync?.status}
                            >
                                <div className="mt-3 grid grid-cols-2 gap-2 border-t border-outline-variant pt-3 text-sm text-on-surface-variant">
                                    <div>Tenants with errors: <span className="font-bold text-on-surface">{health.sync?.tenantsWithErrorsCount}</span></div>
                                    <div>Monitors with errors: <span className="font-bold text-on-surface">{health.sync?.monitorsWithErrorsCount}</span></div>
                                </div>
                                {health.sync?.globalSyncError && (
                                    <div className="mt-2 rounded-lg bg-error-container p-3 text-sm font-medium leading-5 text-on-error-container">
                                        {health.sync.globalSyncError}
                                    </div>
                                )}
                            </HealthStatusCard>

                            {/* Hangfire Background Health Card */}
                            <HealthStatusCard
                                title="Hangfire Status"
                                subtitle="Scheduler worker queues"
                                status={health.hangfire?.status}
                            >
                                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 mt-2 text-center border-t border-outline-variant pt-3 text-sm text-on-surface-variant">
                                    <div className="flex flex-col rounded bg-surface-container-high p-1.5">
                                        <span className="font-extrabold text-slate-850">{health.hangfire?.processingCount}</span>
                                        <span>Active</span>
                                    </div>
                                    <div className="flex flex-col rounded bg-surface-container-high p-1.5">
                                        <span className="font-extrabold text-slate-850">{health.hangfire?.enqueuedCount}</span>
                                        <span>Queued</span>
                                    </div>
                                    <div className="flex flex-col rounded bg-surface-container-high p-1.5">
                                        <span className="font-extrabold text-slate-850">{health.hangfire?.scheduledCount}</span>
                                        <span>Scheduled</span>
                                    </div>
                                    <div className="flex flex-col rounded bg-surface-container-high p-1.5">
                                        <span className={`font-extrabold ${health.hangfire?.failedCount > 0 ? 'text-red-650' : 'text-slate-850'}`}>{health.hangfire?.failedCount}</span>
                                        <span>Failed</span>
                                    </div>
                                </div>
                            </HealthStatusCard>

                            <div className="flex flex-wrap gap-3">
                                <SecureButton
                                    onClick={openDashboard}
                                    locked={!isAdmin}
                                    lockTitle="Requires Admin privileges"
                                    loading={dashboardSessionMutation.isPending}
                                    loadingText="Opening Dashboard..."
                                    className="flex min-h-11 w-fit cursor-pointer items-center justify-center gap-2 rounded-full border border-outline enabled:hover:bg-surface-container px-5 text-sm font-bold text-on-surface transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
                                >
                                    Open Hangfire Dashboard
                                </SecureButton>
                                <SecureButton
                                    onClick={() => clearErrorsMutation.mutate()}
                                    locked={!isAdmin}
                                    lockTitle="Requires Admin privileges"
                                    loading={clearErrorsMutation.isPending}
                                    loadingText="Clearing Diagnostics..."
                                    className="flex min-h-11 w-fit cursor-pointer items-center justify-center gap-2 rounded-full border border-outline enabled:hover:bg-surface-container px-5 text-sm font-bold text-on-surface transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
                                >
                                    Clear Sync Errors
                                </SecureButton>
                            </div>
                        </div>
                    ) : isLoadingHealth ? (
                        <div className="flex flex-col gap-8 shrink-0">
                            <Skeleton.Card className="h-16" />
                            <Skeleton.Card className="h-28" />
                            <Skeleton.Card className="h-28" />
                        </div>
                    ) : isHealthError ? (
                        <div role="alert" className="flex min-h-48 items-center justify-center p-4 text-center text-on-surface-variant">
                            Unable to load pipeline health.
                        </div>
                    ) : null}

                    {/* Sync Dates Section */}
                    {health && (
                        <div className="flex shrink-0 flex-col overflow-hidden rounded-xl bg-surface-container">
                            <div className="border-b border-outline-variant p-4 text-sm font-black uppercase tracking-wide text-on-surface-variant">
                                Last Successful Syncs
                            </div>
                            <div className="flex flex-col divide-y overflow-y-auto divide-slate-100 text-sm">
                                <div className="flex justify-between items-center p-3">
                                    <span className="font-semibold text-on-surface-variant">Litium Ingestion</span>
                                    <span className="font-bold text-on-surface">{timeAgo(health.lastLitiumSync)}</span>
                                </div>
                                <div className="flex justify-between items-center p-3">
                                    <span className="font-semibold text-on-surface-variant">Fleet Meta Update</span>
                                    <span className="font-bold text-on-surface">{timeAgo(health.lastFleetUpdate)}</span>
                                </div>
                                <div className="flex justify-between items-center p-3">
                                    <span className="font-semibold text-on-surface-variant">Uptimerobot Sync</span>
                                    <span className="font-bold text-on-surface">{timeAgo(health.lastFleetUptimeUpdate)}</span>
                                </div>
                                <div className="flex justify-between items-center p-3">
                                    <span className="font-semibold text-on-surface-variant">Latencyrobot Sync</span>
                                    <span className="font-bold text-on-surface">{timeAgo(health.lastFleetLatencyUpdate)}</span>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </SettingsPanel>

            {/* System Logs console */}
            <ConsolePanel
                title="System events"
                subtitle="Application and synchronization diagnostics"
                icon={<TerminalSquare size={18} />}
                className="landscape-contained:col-span-1 min-h-[400px] max-h-[500px] contained:max-h-none"
            >
                {isLoadingEvents ? (
                    <ConsoleLoadingRows label="Loading system events" />
                ) : isEventsError ? (
                    <div role="alert" className="flex h-full items-center justify-center p-4 text-center text-console-text">
                        Unable to load system events.
                    </div>
                ) : events?.length ? (
                    <div className="flex flex-col min-w-0">
                        {events.map((e: SystemEvent, i: number) => (
                        <LogEventRow key={e.id || i} e={e} />
                        ))}
                    </div>
                ) : (
                    <div className="flex h-full items-center justify-center p-4 text-center text-console-text">
                        No system events found.
                    </div>
                )}
            </ConsolePanel>
        </div>
    );
}

function LogEventRow({ e }: { e: SystemEvent }) {
    const [copied, setCopied] = useState(false);

    const levelStr = (() => {
        if (e.level === undefined || e.level === null) return 'info';
        if (typeof e.level === 'number') {
            const map: Record<number, string> = {
                0: 'info',
                1: 'warning',
                2: 'error',
                3: 'critical'
            };
            return map[e.level] || `level-${e.level}`;
        }
        return String(e.level);
    })();

    const levelLower = levelStr.toLowerCase();
    const isError = levelLower === 'error' || levelLower === 'critical' || levelLower.includes('error') || levelLower.includes('critical') || levelLower.includes('4') || levelLower.includes('5');
    const isWarn = levelLower === 'warning';

    let displayMessage = e.message;
    if (e.tenant && e.tenant.name) {
        const tenantIdStr = String(e.tenant.id || e.tenantId);
        const regex = new RegExp(tenantIdStr, 'gi');
        displayMessage = displayMessage.replace(regex, e.tenant.name);
    }

    const handleCopy = () => {
        const d = new Date(e.timestamp);
        const year = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        const time = formatDateTime(d, { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
        const dateTimeStr = `${year}-${month}-${day} ${time}`;
        const levelStrFormatted = `[${levelStr.toUpperCase()}]`;
        
        let fullText = `${dateTimeStr} ${levelStrFormatted}`;
        if (e.source) {
            fullText += ` [Source: ${e.source}]`;
        }
        fullText += ` ${displayMessage}`;
        if (e.details && e.details !== e.message) {
            fullText += `\nDetails: ${e.details}`;
        }
        if (e.exception) {
            fullText += `\nException: ${e.exception}`;
        }

        navigator.clipboard.writeText(fullText).then(() => {
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        });
    };

    return (
        <div className="group relative flex flex-col rounded px-3 py-1 transition-colors hover:bg-console-hover select-text min-w-0">
            {/* First Row: Date, Level icon, Level prefix, Message */}
            <div className="flex items-center flex-wrap gap-x-4 gap-y-2 pr-8 min-w-0">
                <span className="shrink-0 font-mono text-sm">
                    {(() => {
                        const d = new Date(e.timestamp);
                        const year = d.getFullYear();
                        const month = String(d.getMonth() + 1).padStart(2, '0');
                        const day = String(d.getDate()).padStart(2, '0');
                        const time = formatDateTime(d, { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
                        return `${year}-${month}-${day} ${time}`;
                    })()}
                </span>
                {isError ? <AlertCircle size={13} className="shrink-0 text-console-red" /> :
                    isWarn ? <AlertTriangle size={13} className="shrink-0 text-console-yellow" /> :
                        <Info size={13} className="shrink-0 text-console-blue" />}
                <span className={`shrink-0 text-sm font-bold ${isError ? 'text-console-red' : isWarn ? 'text-console-yellow' : 'text-console-blue'}`}>
                    [{levelStr.toUpperCase()}]
                </span>
                <span className="break-words leading-tight">{displayMessage}</span>
            </div>

            {/* Second Row: Badges (Source, Tenant) */}
            {(e.source || e.tenant?.name) && (
                <div className="flex flex-wrap items-center gap-x-8 gap-y-2 text-sm font-medium">
                    {e.source && (
                        <span>Source: <strong className="">{e.source}</strong></span>
                    )}
                    {e.tenant?.name && (
                        <span className="text-console-icon">Tenant: <strong className="">{e.tenant.name}</strong></span>
                    )}
                </div>
            )}

            {/* Third Row: Details */}
            {e.details && e.details !== e.message && (
                <div className="whitespace-pre-wrap rounded border border-console-border bg-console-hover p-2 text-sm text-on-surface-variant">
                    {e.details}
                </div>
            )}

            {/* Fourth Row: Exception */}
            {e.exception && (
                <div className="custom-scrollbar overflow-x-auto rounded border border-console-red/40 bg-console-error-bg p-2 text-sm text-console-error-text min-w-0">
                    <pre className="whitespace-pre-wrap break-all">{e.exception}</pre>
                </div>
            )}

            {/* Copy Button (visible on hover) */}
            <button
                onClick={handleCopy}
                aria-label="Copy log entry"
                className="absolute right-2 top-2 cursor-pointer rounded bg-console-hover p-1 text-on-surface-variant opacity-0 transition-[background-color,color,opacity] duration-150 hover:bg-console-hover-dark hover:text-console-text group-hover:opacity-100"
                title="Copy full log entry"
            >
                {copied ? <Check size={20} className="text-console-green" /> : <Copy size={20} />}
            </button>
        </div>
    );
}
