// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { PlatformConfigurationView } from './platform';
import type { GlobalConfigDto } from '@types';

const testState = vi.hoisted(() => ({
  config: { systemEventRetentionDays: 30, visibleRecurringJobs: ['SystemEventCleanup'] } as GlobalConfigDto,
  updateConfig: vi.fn(),
  recurringJobs: [
    { id: 'system-event-cleanup', kind: 'SystemEventCleanup', name: 'System Event Cleanup', platformWide: true },
    { id: 'refresh-financial-materialized-views', kind: 'FinancialViewRefresh', name: 'Financial View Refresh', platformWide: true },
    { id: 'dispatch-order-fetch-00000000-0000-0000-0000-00000000000a', kind: 'OrderFetch', name: 'Order Fetch', platformWide: false },
  ],
}));

vi.mock('../../hooks/useJobSettingsQueries', () => ({
  useGlobalConfigQuery: () => ({ data: testState.config }),
  useRecurringJobsQuery: () => ({ data: testState.recurringJobs }),
  useUpdateConfigMutation: () => ({ mutateAsync: testState.updateConfig }),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: 'Admin' }),
}));

vi.mock('../../api/generated/endpoints', () => ({
  usePostApiDashboardSession: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

vi.mock('../../hooks/useOrganizationQueries', () => ({
  useOrganizationSummariesQuery: () => ({ data: [], isLoading: false, isError: false }),
  useCreateOrganizationMutation: () => ({ mutate: vi.fn(), isPending: false }),
  useRenameOrganizationMutation: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteOrganizationMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

describe('PlatformConfigurationView', () => {
  it('exposes the Hangfire dashboard entry point', () => {
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('heading', { name: 'Platform' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Open Hangfire Dashboard/ })).toBeEnabled();
  });

  it('shows only platform-wide jobs and marks the managed ones', () => {
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('heading', { name: 'Visible Scheduled Jobs' })).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /System Event Cleanup/ })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: /Financial View Refresh/ })).not.toBeChecked();
    expect(screen.queryByRole('checkbox', { name: /Order Fetch/ })).not.toBeInTheDocument();
  });

  it('shows newly registered platform jobs without a catalog change', () => {
    testState.recurringJobs = [...testState.recurringJobs, { id: 'new-platform-job', kind: 'NewPlatformJob', name: 'new-platform-job', platformWide: true }];
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('checkbox', { name: /new-platform-job/ })).not.toBeChecked();
  });

  it('persists a visibility change through the config update', () => {
    render(<PlatformConfigurationView />, { wrapper });

    fireEvent.click(screen.getByRole('checkbox', { name: /Financial View Refresh/ }));

    expect(testState.updateConfig).toHaveBeenCalledWith({
      visibleRecurringJobs: ['SystemEventCleanup', 'FinancialViewRefresh'],
    });
  });

  it('removes a job from visibility when unchecked', () => {
    render(<PlatformConfigurationView />, { wrapper });

    fireEvent.click(screen.getByRole('checkbox', { name: /System Event Cleanup/ }));

    expect(testState.updateConfig).toHaveBeenCalledWith({ visibleRecurringJobs: [] });
  });
});