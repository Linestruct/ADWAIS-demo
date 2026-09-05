// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { TenantResponseDto, UptimeMonitorDto } from '@types';
import { TenantMonitorsPanel } from '../../components/settings/tenants/TenantMonitorsPanel';
import { MonitorAssignmentPanel } from './MonitorDetail';

const testState = vi.hoisted(() => ({
  navigate: vi.fn(),
  viewModel: {} as Record<string, unknown>,
}));

vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...await importOriginal<typeof import('@tanstack/react-router')>(),
  useNavigate: () => testState.navigate,
}));

vi.mock('../../hooks/useTenantsViewModel', () => ({
  useTenantsViewModel: () => testState.viewModel,
}));

const monitor: UptimeMonitorDto = {
  id: 42,
  tenantId: 'tenant-1',
  tenantName: 'Acme',
  type: 'Ping',
  name: 'Health check',
  url: 'https://example.com/health',
  updateInterval: 60,
  latencyDegradedFloor: null,
  uptimeSla: null,
  currentUptimePercentage: null,
  currentLatency: null,
  uptimeMonitorEnabled: true,
  currentStatus: 'UP',
  lastUpdate: null,
  lastUptimeUpdate: null,
  lastLatencyUpdate: null,
  createdDate: '2026-01-01T00:00:00Z',
  lastSyncError: null,
  tags: [],
  tenantBaseUrl: 'https://example.com',
  tenantImageUrl: null,
  httpMethod: null,
  timeoutSeconds: null,
  sslExpiresAt: null,
  domainExpiresAt: null,
  monitoredRegions: [],
  currentStateDurationSeconds: null,
  latestIncident: null,
};

const tenant: TenantResponseDto = {
  id: 'tenant-1',
  name: 'Acme',
  type: 'B2B',
  orderProvider: 'litium',
  orderProviderSettings: { endpointUrl: 'https://example.com' },
  orderProviderConfiguredSecretKeys: ['authorization'],
  imageUrl: null,
  currentlyFetching: false,
  fetchedFrom: null,
  fetchedUntil: null,
  lastPolled: null,
  orderFetchingEnabled: true,
  monitorCount: 1,
  lastSyncError: null,
  hasOrderProviderSettings: true,
};

const mutation = () => ({ isPending: false, mutate: vi.fn() });

describe('settings cross-navigation', () => {
  beforeEach(() => {
    testState.navigate.mockReset();
  });

  it('opens a monitor from a tenant on row double-click', () => {
    testState.viewModel = {
      allMonitors: [monitor],
      allUniqueTypes: ['Ping'],
      allUniqueTags: [],
      assignMonitor: mutation(),
      unassignMonitor: mutation(),
      isAdmin: true,
    };

    render(<TenantMonitorsPanel tenantId={tenant.id} />);
    const row = screen.getByText(monitor.name).closest('tr');
    if (!row) throw new Error('Monitor row not found');
    fireEvent.doubleClick(row);

    expect(testState.navigate).toHaveBeenCalledWith({
      to: '/settings/monitors/$monitorId',
      params: { monitorId: String(monitor.id) },
    });
  });

  it('opens the assigned tenant directly from a monitor', () => {
    testState.viewModel = {
      tenants: [tenant],
      assignMonitor: mutation(),
      unassignMonitor: mutation(),
    };

    render(<MonitorAssignmentPanel monitor={monitor} isAdmin />);
    fireEvent.click(screen.getByRole('button', { name: /Acme/ }));

    expect(testState.navigate).toHaveBeenCalledWith({
      to: '/settings/tenants/$tenantId',
      params: { tenantId: tenant.id },
    });
  });
});
