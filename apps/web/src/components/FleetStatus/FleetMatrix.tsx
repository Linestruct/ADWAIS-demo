// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import type { UptimeMonitorDto } from '@types';
import { getMonitorStatus, STATUS_THEMES } from "../../utils/monitorStatusHelper.ts";
import { getTenantFaviconUrl } from "../../utils/tenantHelper.ts";
import { getTagColor, getTagStyle } from "../../utils/tagHelper.ts";
import { getMonitorType } from '../../utils/monitorTypeHelper.ts';

function FleetMatrixTile({
  monitor,
  isActive,
  selectedMonitorId,
  onMonitorSelect,
}: {
  monitor: UptimeMonitorDto;
  isActive: boolean;
  selectedMonitorId?: number | null;
  onMonitorSelect?: (monitor: UptimeMonitorDto) => void;
}) {
  const [imgError, setImgError] = useState(false);

  const status = getMonitorStatus(monitor.currentStatus, monitor.currentLatency, monitor.latencyDegradedFloor);
  const tenantDisplay = monitor.tenantName || monitor.name.split('-')[0]?.trim() || 'Tenant';
  const displayUrl = monitor.url.replace(/^(?:[^:]+:\/\/)?(?:www\.)?/, '');
  const theme = STATUS_THEMES[status];
  const faviconUrl = monitor.tenantImageUrl
    || getTenantFaviconUrl(monitor.tenantBaseUrl || monitor.url);
  const showLetter = !faviconUrl || imgError;

  return (
    <button
      type="button"
      onClick={() => onMonitorSelect?.(monitor)}
      className={`flex-1 min-w-[225px] max-w-[450px] p-3 rounded-lg transition-all text-left border-2 relative overflow-hidden group min-h-22.5 shrink-0 flex flex-col justify-between min-w-0
        ${theme.bg} ${theme.border} ${theme.text}
        ${isActive ? 'z-10 m3-elevation-3' : 'm3-elevation-2 hover:m3-elevation-3'}
        ${selectedMonitorId && !isActive ? 'opacity-30' : 'opacity-100'}
        ${!monitor.uptimeMonitorEnabled ? 'grayscale opacity-50' : ''}
      `}
    >
      <div className="gap-2 relative z-10 flex flex-col h-full justify-between w-full min-w-0">
        <div className="flex flex-col w-full min-w-0">
          <div className={"flex flex-1 gap-2 border-b border-outline-variant pb-1 mb-2 items-start"}>
          <span className={`w-3 h-3 rounded-full shrink-0 mt-0.5 ${theme.dot}`} aria-label={status} />
          <span className={`text-sm font-black ${theme.text} break-all tracking-tight line-clamp-2 h-[calc(2*1.25em)] leading-tight`}>
            {displayUrl}
          </span>
          </div>
          
          <div className="flex items-start justify-between gap-2 mt-0.5 min-w-0">
            <span className={`text-xs font-bold ${theme.mutedText} tracking-wider min-w-0 break-words`}>
              <span className="uppercase">{getMonitorType(monitor.type)}</span>
            </span>
          </div>
        </div>

        {monitor.tags && monitor.tags.length > 0 && (
          <div className="flex flex-wrap gap-2 w-full min-w-0 border-b pb-2">
            {monitor.tags.map((tag) => {
              const name = tag.split(':')[0].trim();
              const color = getTagColor(tag);
              return (
                <span
                  key={tag}
                  className={`text-xs font-bold px-1.5 py-0.5 rounded uppercase tracking-wider border ${getTagStyle(color)}`}
                >
                  {name}
                </span>
              );
            })}
          </div>
        )}

        <div className="grid grid-cols-3 items-end mt-auto w-full items-center min-w-0 gap-2">
          <div className="flex flex-col gap-0 min-w-0">
            <span className={`text-xs ${theme.mutedText} uppercase font-bold tracking-wider truncate`}>Uptime</span>
            <span className={`text-sm font-black ${theme.valueText} truncate`}>
              {monitor.currentUptimePercentage != null ? `${monitor.currentUptimePercentage.toFixed(2)}%` : 'N/A'}
            </span>
          </div>
          
          <div className="min-w-0 pointer-events-none flex justify-center items-center select-none">
            {showLetter ? (
                <span className={`text-5xl font-black opacity-10 select-none leading-none ${theme.text}`}>
            {tenantDisplay.charAt(0).toUpperCase()}
          </span>
            ) : (
                <img
                    src={faviconUrl!}
                    alt=""
                    className="w-12 h-12 object-contain opacity-30 mix-blend-multiply"
                    onError={() => setImgError(true)}
                />
            )}
          </div>
          <div className="flex flex-col items-end gap-0 min-w-0 text-right">
            <span className={`text-xs ${theme.mutedText} uppercase font-bold tracking-wider truncate`}>Latency</span>
            <span className={`text-sm font-black ${theme.valueText} truncate`}>
              {(status === 'down' || status === 'unknown' || status === 'paused' || status === 'starting' || !Number(monitor.currentLatency)) ? 'N/A' : `${Math.round(Number(monitor.currentLatency))}ms`}
            </span>
          </div>
        </div>
        
          
      </div>
    </button>
  );
}

export function FleetMatrix({
  monitors,
  onMonitorSelect,
  selectedMonitorId
}: {
  monitors: UptimeMonitorDto[],
  onMonitorSelect?: (monitor: UptimeMonitorDto) => void,
  selectedMonitorId?: number | null
}) {
  const sortedMonitors = [...monitors].sort((a, b) => {
    const sA = getMonitorStatus(a.currentStatus, a.currentLatency, a.latencyDegradedFloor);
    const sB = getMonitorStatus(b.currentStatus, b.currentLatency, b.latencyDegradedFloor);
    const wA = sA === 'down' ? 300 : sA === 'degraded' ? 200 : (a.currentUptimePercentage !== null && a.currentUptimePercentage !== undefined && a.uptimeSla !== null && a.uptimeSla !== undefined && a.currentUptimePercentage < a.uptimeSla) ? 100 : 0;
    const wB = sB === 'down' ? 300 : sB === 'degraded' ? 200 : (b.currentUptimePercentage !== null && b.currentUptimePercentage !== undefined && b.uptimeSla !== null && b.uptimeSla !== undefined && b.currentUptimePercentage < b.uptimeSla) ? 100 : 0;
    
    return wB - wA || (a.currentUptimePercentage ?? 100) - (b.currentUptimePercentage ?? 100);
  });

  return (
    <div className="flex flex-wrap gap-4 justify-center contained:justify-start w-full min-w-0 pt-1 pb-4">
      {sortedMonitors.map((monitor) => (
        <FleetMatrixTile
          key={`${monitor.tenantId}-${monitor.id}`}
          monitor={monitor}
          isActive={selectedMonitorId === monitor.id}
          selectedMonitorId={selectedMonitorId}
          onMonitorSelect={onMonitorSelect}
        />
      ))}
    </div>
  );
}
