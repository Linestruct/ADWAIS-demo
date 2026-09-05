// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { describe, expect, it } from 'vitest';
import type { UptimeMonitorDto } from '@types';
import { filterFleetMonitors, getFleetSettingsTarget, getFleetTags, isFleetSelectionVisible } from './fleetFilters';

const monitor = (
  id: number,
  tags: string[],
  currentStatus: string,
): UptimeMonitorDto => ({
  id,
  tenantId: `tenant-${id}`,
  name: `Monitor ${id}`,
  tags,
  currentStatus,
  uptimeMonitorEnabled: true,
} as UptimeMonitorDto);

describe('fleet filters', () => {
  const monitors = [
    monitor(1, ['prod', 'dev'], 'UP'),
    monitor(2, ['prod'], 'DOWN'),
    monitor(3, ['status:paused'], 'PAUSED'),
  ];

  it('keeps tag options independent from the filtered result', () => {
    expect(getFleetTags(monitors)).toEqual(['dev', 'prod', 'status:paused']);
    expect(getFleetTags(filterFleetMonitors(monitors, {
      includedTags: ['dev'],
      excludedTags: [],
      hiddenStatuses: [],
    })))
      .toEqual(['dev', 'prod']);
  });

  it('matches any selected tag within the tag facet', () => {
    expect(filterFleetMonitors(monitors, {
      includedTags: ['prod', 'dev'],
      excludedTags: [],
      hiddenStatuses: [],
    }).map(({ id }) => id))
      .toEqual([1, 2]);
  });

  it('matches tags case-insensitively without discarding namespaced values', () => {
    expect(filterFleetMonitors(monitors, {
      includedTags: ['STATUS:PAUSED'],
      excludedTags: [],
      hiddenStatuses: [],
    }).map(({ id }) => id))
      .toEqual([3]);
  });

  it('gives excluded tags precedence and hides selected statuses', () => {
    expect(filterFleetMonitors(monitors, {
      includedTags: ['prod'],
      excludedTags: ['dev'],
      hiddenStatuses: ['PAUSED'],
    }).map(({ id }) => id))
      .toEqual([2]);
  });

  it('reports when a selected tenant or monitor is no longer visible', () => {
    const visibleMonitors = filterFleetMonitors(monitors, {
      includedTags: ['dev'],
      excludedTags: [],
      hiddenStatuses: [],
    });

    expect(isFleetSelectionVisible(visibleMonitors, { tenantId: 'tenant-1', monitorId: 1 })).toBe(true);
    expect(isFleetSelectionVisible(visibleMonitors, { tenantId: 'tenant-1', monitorId: null })).toBe(true);
    expect(isFleetSelectionVisible(visibleMonitors, { tenantId: 'tenant-2', monitorId: 2 })).toBe(false);
  });

  it('maps every fleet scope to its matching settings route and label', () => {
    expect(getFleetSettingsTarget(null)).toEqual({
      label: 'Monitor settings',
      to: '/settings/monitors',
    });
    expect(getFleetSettingsTarget({ tenantId: 'tenant-1', monitorId: null })).toEqual({
      label: 'Edit tenant',
      to: '/settings/tenants/$tenantId',
      params: { tenantId: 'tenant-1' },
    });
    expect(getFleetSettingsTarget({ tenantId: 'tenant-1', monitorId: 42 })).toEqual({
      label: 'Edit monitor',
      to: '/settings/monitors/$monitorId',
      params: { monitorId: '42' },
    });
  });
});
