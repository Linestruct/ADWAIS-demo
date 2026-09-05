// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { MonitorAvailabilitySeriesResponseDto, UptimeMonitorDto } from '@types';
import type { ReactNode } from 'react';
import type { FleetSelection } from '../../utils/fleetFilters';
import { Settings } from 'lucide-react';
import { getMonitorStatus, normalizeStatus } from '../../utils/monitorStatusHelper';
import { getMonitorType } from '../../utils/monitorTypeHelper';
import { CollectionPanel } from '../common/dashboard/CollectionPanel';
import { Button } from '../common/ui/Button';
import { ErrorAlert } from '../common/ui/ErrorAlert';
import { AvailabilityStrip } from './AvailabilityStrip';
import { formatDateTime } from '../../utils/dateTime';

function formatPercent(value: number | null | undefined) {
  return value == null ? 'N/A' : `${value.toFixed(3)}%`;
}

function formatLatency(value: number | null | undefined) {
  return value == null || value === 0 ? 'N/A' : `${Math.round(value)}ms`;
}

function formatDuration(seconds: number | null | undefined) {
  if (seconds == null) return 'Not reported';
  if (seconds < 60) return `${seconds}s`;

  const days = Math.floor(seconds / 86_400);
  const hours = Math.floor((seconds % 86_400) / 3_600);
  const minutes = Math.floor((seconds % 3_600) / 60);
  return [days > 0 ? `${days}d` : null, hours > 0 ? `${hours}h` : null, minutes > 0 ? `${minutes}m` : null]
    .filter(Boolean)
    .slice(0, 2)
    .join(' ');
}

function formatMetadataDate(value: string | null | undefined, includeTime = false) {
  if (!value) return 'Not reported';
  return formatDateTime(value, includeTime
    ? { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }
    : { day: 'numeric', month: 'short', year: 'numeric' }, 'en-SE');
}

function MetadataItem({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-sm font-bold uppercase tracking-wide text-on-surface-variant">{label}</dt>
      <dd className="mt-0.5 text-sm font-bold text-on-surface break-words">{value}</dd>
    </div>
  );
}

function Fact({ label, value, detail }: { label: string; value: ReactNode; detail?: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 rounded-xl bg-surface-container p-3 min-w-0">
      <span className="text-sm font-bold uppercase tracking-wide text-on-surface-variant">{label}</span>
      <span className="text-xl font-black text-on-surface break-words">{value}</span>
      {detail && <div className="text-base font-bold text-on-surface-variant break-words">{detail}</div>}
    </div>
  );
}

function Trend({ value, inverse = false }: { value: number | null | undefined; inverse?: boolean }) {
  if (value == null) return null;

  const isFavorable = inverse ? value <= 0 : value >= 0;
  const colorClass = value === 0
    ? 'text-on-surface-variant'
    : isFavorable
      ? 'text-status-up'
      : 'text-status-down';
  const sign = value > 0 ? '+' : '';

  return <span className={colorClass}>{sign}{value.toFixed(2)}% vs previous</span>;
}

export function FleetSelectionPanel({
  selection,
  selectedTenantName,
  selectedMonitor,
  scopedMonitors,
  availability,
  isLoading,
  isError,
  isStale,
  averageLatency,
  p10Latency,
  p90Latency,
  uptimeGrowth,
  latencyGrowth,
  p10LatencyGrowth,
  p90LatencyGrowth,
  onOpenSettings,
  settingsLabel,
  className,
}: {
  selection: FleetSelection | null;
  selectedTenantName: string | null;
  selectedMonitor: UptimeMonitorDto | null;
  scopedMonitors: UptimeMonitorDto[];
  availability?: MonitorAvailabilitySeriesResponseDto;
  isLoading?: boolean;
  isError?: boolean;
  isStale?: boolean;
  averageLatency?: number | null;
  p10Latency?: number | null;
  p90Latency?: number | null;
  uptimeGrowth?: number | null;
  latencyGrowth?: number | null;
  p10LatencyGrowth?: number | null;
  p90LatencyGrowth?: number | null;
  onOpenSettings: () => void;
  settingsLabel: string;
  className?: string;
}) {
  const enabledMonitors = scopedMonitors.filter(monitor => monitor.uptimeMonitorEnabled);
  const downCount = enabledMonitors.filter(monitor => {
    const status = normalizeStatus(monitor.currentStatus);
    return status === 'DOWN' || status === 'CRITICAL';
  }).length;
  const degradedCount = enabledMonitors.filter(monitor =>
    getMonitorStatus(
      monitor.currentStatus,
      monitor.currentLatency,
      monitor.latencyDegradedFloor,
    ) === 'degraded',
  ).length;
  const monitorSla = selectedMonitor?.uptimeSla ?? null;
  const title = selectedMonitor
    ? 'Endpoint details'
    : selection
      ? 'Tenant details'
      : 'Fleet overview';

  const status = selectedMonitor
    ? getMonitorStatus(
        selectedMonitor.currentStatus,
        selectedMonitor.currentLatency,
        selectedMonitor.latencyDegradedFloor,
      )
    : null;
  const statusLabel = status?.toUpperCase();
  const statusClass = status === 'down'
    ? 'bg-status-down text-white'
    : status === 'degraded'
      ? 'bg-status-degraded text-white'
      : 'bg-teal-600 text-white';

  return (
    <CollectionPanel
      title={title}
      className={className}
      isLoading={isLoading && !availability}
      titleClassName="!text-sm md:!text-base"
      actions={(
        <div className="flex items-center gap-2">
          {statusLabel && (
            <span className={`rounded-md px-2 py-1 text-sm font-black tracking-wide ${statusClass}`}>
              {statusLabel}
            </span>
          )}
          <Button
            variant="tonal"
            color="secondary"
            icon={<Settings size={18} />}
            aria-label={settingsLabel}
            title={settingsLabel}
            onClick={onOpenSettings}
          >
            {settingsLabel}
          </Button>
        </div>
      )}
    >
      {isError ? (
        <div className="p-4"><ErrorAlert title={`${title} unavailable`} message={`${title} is temporarily unavailable.`} /></div>
      ) : (
      <div className={`flex flex-col gap-4 px-4 pb-4 ${isStale ? 'opacity-70' : ''}`}>
        <div className="min-w-0">
          {selectedMonitor ? (
            <>
              <p className="text-base font-black text-on-surface break-all leading-tight">{selectedMonitor.url}</p>
              <p className="mt-1 text-base font-bold text-on-surface-variant break-words">
                {getMonitorType(selectedMonitor.type)} · {selectedTenantName}
              </p>
            </>
          ) : selection ? (
            <>
              <p className="text-xl font-black text-on-surface break-words">{selectedTenantName}</p>
              <p className="mt-1 text-base font-medium text-on-surface-variant">
                Aggregated facts for the currently visible tenant monitors.
              </p>
            </>
          ) : (
            <>
              <p className="text-xl font-black text-on-surface">Current fleet</p>
              <p className="mt-1 text-base font-medium text-on-surface-variant">
                Select a tenant or endpoint for more specific facts.
              </p>
            </>
          )}
        </div>

        <div className="flex flex-col gap-2 min-w-0">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <h3 className="text-base font-bold uppercase tracking-wide text-on-surface-variant">Availability over time</h3>
            <span className="text-sm font-bold text-on-surface-variant">Selected period</span>
          </div>
          <AvailabilityStrip
              points={availability?.points ?? []}
              sla={monitorSla}
              aggregate={!selectedMonitor}
          />
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
          {selectedMonitor ? (
            <>
              <Fact
                label="Period uptime"
                value={formatPercent(availability?.averageUptimePercentage)}
                detail={(
                  <div className="flex flex-wrap gap-x-2">
                    <span>{monitorSla != null ? `SLA ${monitorSla.toFixed(3)}%` : 'SLA not configured'}</span>
                    <Trend value={uptimeGrowth} />
                  </div>
                )}
              />
              <Fact
                label="Current latency"
                value={formatLatency(selectedMonitor.currentLatency)}
                detail={selectedMonitor.latencyDegradedFloor != null ? `Threshold ${selectedMonitor.latencyDegradedFloor}ms` : 'Threshold not configured'}
              />
              <Fact label="Average latency" value={formatLatency(averageLatency)} detail={<Trend value={latencyGrowth} inverse />} />
              <Fact label="P90 latency" value={formatLatency(p90Latency)} detail={<Trend value={p90LatencyGrowth} inverse />} />
              <Fact label="P10 latency" value={formatLatency(p10Latency)} detail={<Trend value={p10LatencyGrowth} inverse />} />
              <Fact label="Lowest day" value={formatPercent(availability?.lowestUptimePercentage)} />
            </>
          ) : (
            <>
              <Fact label="Period uptime" value={formatPercent(availability?.averageUptimePercentage)} detail={<Trend value={uptimeGrowth} />} />
              <Fact label="Average latency" value={formatLatency(averageLatency)} detail={<Trend value={latencyGrowth} inverse />} />
              <Fact label="P90 latency" value={formatLatency(p90Latency)} detail={<Trend value={p90LatencyGrowth} inverse />} />
              <Fact label="P10 latency" value={formatLatency(p10Latency)} detail={<Trend value={p10LatencyGrowth} inverse />} />
              <Fact label="Monitors" value={enabledMonitors.length.toLocaleString('en-SE')} />
              <Fact
                label="Active incidents"
                value={(downCount + degradedCount).toLocaleString('en-SE')}
                detail={(
                  <span className="flex flex-wrap gap-x-2">
                    <span className="text-status-down">{downCount} down</span>
                    <span className="text-status-degraded">{degradedCount} degraded</span>
                  </span>
                )}
              />
            </>
          )}
        </div>

        {selectedMonitor && (
          <div className="flex flex-col gap-3 rounded-xl border border-outline-variant bg-surface-container-low p-3">
            <dl className="grid grid-cols-2 sm:grid-cols-3 gap-x-3 gap-y-3">
              <MetadataItem label="Interval" value={`${selectedMonitor.updateInterval}s`} />
              <MetadataItem label="HTTP method" value={selectedMonitor.httpMethod || 'Default'} />
              <MetadataItem label="Timeout" value={selectedMonitor.timeoutSeconds != null ? `${selectedMonitor.timeoutSeconds}s` : 'Not reported'} />
              <MetadataItem label="Regions" value={selectedMonitor.monitoredRegions?.length ? selectedMonitor.monitoredRegions.join(', ').toUpperCase() : 'Not reported'} />
              <MetadataItem label="Created" value={formatMetadataDate(selectedMonitor.createdDate)} />
              <MetadataItem label="Current state duration" value={formatDuration(selectedMonitor.currentStateDurationSeconds)} />
              <MetadataItem label="SSL expires" value={formatMetadataDate(selectedMonitor.sslExpiresAt)} />
              <MetadataItem label="Domain expires" value={formatMetadataDate(selectedMonitor.domainExpiresAt)} />
            </dl>

            <div className="border-t border-outline-variant pt-3">
              <p className="text-sm font-bold uppercase tracking-wide text-on-surface-variant">Latest incident</p>
              {selectedMonitor.latestIncident ? (
                <div className="mt-1 flex flex-col gap-0.5">
                  <p className="text-sm font-black text-on-surface break-words">
                    {selectedMonitor.latestIncident.reason || selectedMonitor.latestIncident.cause || 'Incident reported'}
                  </p>
                  <p className="text-sm font-medium text-on-surface-variant">
                    {selectedMonitor.latestIncident.status || 'Unknown status'}
                    {selectedMonitor.latestIncident.startedAt ? ` · ${formatMetadataDate(selectedMonitor.latestIncident.startedAt, true)}` : ''}
                    {selectedMonitor.latestIncident.durationSeconds != null ? ` · ${formatDuration(selectedMonitor.latestIncident.durationSeconds)}` : ''}
                  </p>
                </div>
              ) : (
                <p className="mt-1 text-sm font-medium text-on-surface-variant">No incident reported.</p>
              )}
            </div>
          </div>
        )}
      </div>
      )}
    </CollectionPanel>
  );
}
