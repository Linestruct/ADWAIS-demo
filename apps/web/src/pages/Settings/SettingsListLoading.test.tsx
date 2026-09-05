// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MonitorsView } from './monitors';
import { TenantsMonitorsView } from './tenants';
import { UsersView } from './users';

const testState = vi.hoisted(() => ({
  viewModel: {} as Record<string, unknown>,
  usersQuery: {} as Record<string, unknown>,
}));

vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...await importOriginal<typeof import('@tanstack/react-router')>(),
  useNavigate: () => vi.fn(),
}));

vi.mock('../../hooks/useTenantsViewModel', () => ({
  useTenantsViewModel: () => testState.viewModel,
}));

vi.mock('../../hooks/useUserQueries', () => ({
  useUsersQuery: () => testState.usersQuery,
  useCreateUserMutation: () => ({}),
  useDeleteUserMutation: () => ({}),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: 'Viewer' }),
}));

vi.mock('../../components/settings/tenants/SettingsFilterMenu', () => ({
  TenantSettingsFilterMenu: () => null,
  MonitorSettingsFilterMenu: () => null,
}));

function loadingViewModel() {
  return {
    isAdmin: false,
    tenants: undefined,
    sortedTenants: [],
    isTenantsLoading: true,
    isTenantsError: false,
    allMonitors: [],
    filteredAndSortedMonitors: [],
    isMonitorsLoading: true,
    isMonitorsError: false,
    tenantSearch: '',
    setTenantSearch: vi.fn(),
    tenantFilters: { token: 'all', fetch: 'all' },
    setTenantFilters: vi.fn(),
    tenantSort: 'asc',
    setTenantSort: vi.fn(),
    monitorSearch: '',
    setMonitorSearch: vi.fn(),
    monitorFilters: { assignment: 'all', tag: 'all', status: 'all', type: 'all' },
    setMonitorFilters: vi.fn(),
    monitorSort: 'asc',
    setMonitorSort: vi.fn(),
    allUniqueTags: [],
    allUniqueTypes: [],
  };
}

describe('settings list loading states', () => {
  beforeEach(() => {
    testState.viewModel = loadingViewModel();
    testState.usersQuery = { data: undefined, isLoading: true, isError: false, refetch: vi.fn() };
  });

  it.each([
    ['tenants', TenantsMonitorsView, 'Loading tenants', 'No tenants found'],
    ['monitors', MonitorsView, 'Loading monitors', 'No monitors found'],
    ['users', UsersView, 'Loading users', 'No active users registered.'],
  ])('does not render an empty state while %s are loading', (_entity, View, loadingLabel, emptyMessage) => {
    render(<View />);

    expect(screen.getByLabelText(loadingLabel)).toHaveAttribute('aria-busy', 'true');
    expect(screen.queryByText(emptyMessage)).not.toBeInTheDocument();
  });
});
