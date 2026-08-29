// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { PlatformConfigurationView } from './platform';
import type { GlobalConfigDto } from '@types';

const testState = vi.hoisted(() => ({
  config: { systemEventRetentionDays: 30, visibleRecurringJobs: ['system-event-cleanup'] } as GlobalConfigDto,
  updateConfig: vi.fn(),
}));

vi.mock('../../hooks/useJobSettingsQueries', () => ({
  useGlobalConfigQuery: () => ({ data: testState.config }),
  useUpdateConfigMutation: () => ({ mutateAsync: testState.updateConfig }),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: 'Admin' }),
}));

vi.mock('../../api/generated/endpoints', () => ({
  usePostApiDashboardSession: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

describe('PlatformConfigurationView', () => {
  it('shows the deployment-wide configuration', () => {
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('heading', { name: 'Global Configuration' })).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Organization Configuration' })).not.toBeInTheDocument();
  });

  it('exposes the Hangfire dashboard entry point', () => {
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('heading', { name: 'Hangfire Dashboard' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Open Hangfire Dashboard/ })).toBeEnabled();
  });

  it('shows visible scheduled jobs and marks the managed ones', () => {
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('heading', { name: 'Visible Scheduled Jobs' })).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /System Event Cleanup/ })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: /Financial View Refresh/ })).not.toBeChecked();
  });

  it('persists a visibility change through the config update', () => {
    render(<PlatformConfigurationView />, { wrapper });

    fireEvent.click(screen.getByRole('checkbox', { name: /Financial View Refresh/ }));

    expect(testState.updateConfig).toHaveBeenCalledWith({
      visibleRecurringJobs: ['system-event-cleanup', 'refresh-financial-materialized-views'],
    });
  });

  it('removes a job from visibility when unchecked', () => {
    render(<PlatformConfigurationView />, { wrapper });

    fireEvent.click(screen.getByRole('checkbox', { name: /System Event Cleanup/ }));

    expect(testState.updateConfig).toHaveBeenCalledWith({ visibleRecurringJobs: [] });
  });
});