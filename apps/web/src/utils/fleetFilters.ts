// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { UptimeMonitorDto } from '@types';
import { normalizeStatus } from './monitorStatusHelper';

export interface FleetFilters {
  includedTags: string[];
  excludedTags: string[];
  hiddenStatuses: string[];
}

export interface FleetSelection {
  tenantId: string;
  monitorId: number | null;
}

export type FleetSettingsTarget =
  | { label: 'Monitor settings'; to: '/settings/monitors' }
  | { label: 'Edit tenant'; to: '/settings/tenants/$tenantId'; params: { tenantId: string } }
  | { label: 'Edit monitor'; to: '/settings/monitors/$monitorId'; params: { monitorId: string } };

export function getFleetSettingsTarget(selection: FleetSelection | null): FleetSettingsTarget {
  if (selection?.monitorId != null) {
    return {
      label: 'Edit monitor',
      to: '/settings/monitors/$monitorId',
      params: { monitorId: String(selection.monitorId) },
    };
  }

  if (selection) {
    return {
      label: 'Edit tenant',
      to: '/settings/tenants/$tenantId',
      params: { tenantId: selection.tenantId },
    };
  }

  return { label: 'Monitor settings', to: '/settings/monitors' };
}

const normalizeFilterValue = (value: string) => value.trim().toLocaleLowerCase();

export function getFleetTags(monitors: UptimeMonitorDto[]): string[] {
  const tagsByNormalizedValue = new Map<string, string>();

  for (const monitor of monitors) {
    for (const rawTag of monitor.tags ?? []) {
      const tag = rawTag.trim();
      if (tag) tagsByNormalizedValue.set(normalizeFilterValue(tag), tag);
    }
  }

  return [...tagsByNormalizedValue.values()].sort((a, b) =>
    a.localeCompare(b, undefined, { sensitivity: 'base' }),
  );
}

export function filterFleetMonitors(
  monitors: UptimeMonitorDto[],
  { includedTags, excludedTags, hiddenStatuses }: FleetFilters,
): UptimeMonitorDto[] {
  const includedTagSet = new Set(includedTags.map(normalizeFilterValue));
  const excludedTagSet = new Set(excludedTags.map(normalizeFilterValue));
  const hiddenStatusSet = new Set(hiddenStatuses.map(normalizeStatus));

  if (includedTagSet.size === 0 && excludedTagSet.size === 0 && hiddenStatusSet.size === 0) {
    return monitors;
  }

  return monitors.filter(monitor => {
    const normalizedTags = (monitor.tags ?? []).map(normalizeFilterValue);
    const matchesIncludedTags = includedTagSet.size === 0 || normalizedTags.some(tag =>
      includedTagSet.has(tag),
    );
    const matchesExcludedTags = !normalizedTags.some(tag => excludedTagSet.has(tag));
    const matchesStatus = !hiddenStatusSet.has(normalizeStatus(monitor.currentStatus));

    return matchesIncludedTags && matchesExcludedTags && matchesStatus;
  });
}

export function isFleetSelectionVisible(
  monitors: UptimeMonitorDto[],
  selection: FleetSelection,
): boolean {
  return monitors.some(monitor =>
    monitor.tenantId === selection.tenantId &&
    (selection.monitorId === null || monitor.id === selection.monitorId),
  );
}
