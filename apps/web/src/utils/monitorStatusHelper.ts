// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export function normalizeStatus(status?: string | number | null): string {
    if (status === undefined || status === null) return 'UNKNOWN';
    const s = status.toString().toUpperCase().trim();
    if (s === '1' || s === 'STARTING' || s === 'NOT CHECKED YET' || s === 'NOT CHECKED') return 'STARTING';
    if (s === '2' || s === 'UP') return 'UP';
    if (s === '8' || s === '9' || s === 'DOWN' || s === 'SEEMS DOWN' || s === 'CRITICAL') return 'DOWN';
    if (s === '0' || s === 'PAUSED') return 'PAUSED';
    return 'UNKNOWN';
}

export type MonitorStatus = 'operational' | 'degraded' | 'down' | 'unknown' | 'paused' | 'starting';

export const STATUS_THEMES = {
    down: {
      bg: 'bg-error-container',
      border: 'border-error/40',
      text: 'text-red-600',
      valueText: 'text-red-600',
      mutedText: 'text-slate-500',
      dot: 'bg-red-500'
    },
    degraded: {
      bg: 'bg-amber-50',
      border: 'border-amber-200',
      text: 'text-amber-600',
      valueText: 'text-amber-600',
      mutedText: 'text-slate-500',
      dot: 'bg-amber-500'
    },
    operational: {
      bg: 'bg-surface',
      border: 'border-surface',
      text: '',
      valueText: 'text-slate-600',
      mutedText: 'text-slate-500',
      dot: 'bg-teal-500'
    },
    unknown: {
      bg: 'bg-slate-200',
      border: 'border-slate-300',
      text: 'text-slate-500',
      valueText: 'text-slate-500',
      mutedText: 'text-slate-500',
      dot: 'bg-slate-500'
    },
    paused: {
      bg: 'bg-blue-50',
      border: 'border-blue-200',
      text: 'text-blue-600',
      valueText: 'text-blue-600',
      mutedText: 'text-slate-500',
      dot: 'bg-blue-500'
    },
    starting: {
      bg: 'bg-indigo-50/50',
      border: 'border-indigo-200',
      text: 'text-indigo-900',
      valueText: 'text-indigo-600',
      mutedText: 'text-indigo-500',
      dot: 'bg-indigo-400'
    }
} as const;

export function getMonitorStatus(
  currentStatus: string | number | null | undefined,
  currentLatency: number | null | undefined,
  latencyDegradedFloor: number | null | undefined
): MonitorStatus {
  const status = normalizeStatus(currentStatus);
  if (status === 'STARTING') return 'starting';
  if (status === 'DOWN') return 'down';
  if (status === 'PAUSED') return 'paused';
  if (status === 'UNKNOWN') return 'unknown';

  if (currentLatency !== null && currentLatency !== undefined && latencyDegradedFloor !== null && latencyDegradedFloor !== undefined && currentLatency > latencyDegradedFloor) {
    return 'degraded';
  }

  return 'operational';
}