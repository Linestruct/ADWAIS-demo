// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BackgroundJobsView } from './jobs';

const testState = vi.hoisted(() => ({
  recentJobsQuery: {} as Record<string, unknown>,
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: 'Viewer' }),
}));

vi.mock('../../hooks/useJobSettingsQueries', () => ({
  useRecurringJobsQuery: () => ({ data: [], isLoading: false, isError: false, refetch: vi.fn() }),
  useRecentJobsQuery: () => testState.recentJobsQuery,
  useTriggerJobMutation: () => ({ mutate: vi.fn(), isPending: false }),
  useBackfillMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));

vi.mock('../../hooks/useTenantQueries', () => ({
  useTenantsQuery: () => ({ data: [], isLoading: false, isError: false, refetch: vi.fn() }),
}));

vi.mock('../../components/settings/jobs/RecurringJobsTable', () => ({
  RecurringJobsTable: () => null,
}));

vi.mock('../../components/settings/jobs/ManualBackfillPanel', () => ({
  ManualBackfillPanel: () => null,
}));

describe('recent jobs console state', () => {
  beforeEach(() => {
    testState.recentJobsQuery = { data: undefined, isLoading: true, isError: false, refetch: vi.fn() };
  });

  it('does not report an empty history while loading', () => {
    render(<BackgroundJobsView />);

    expect(screen.getByLabelText('Loading recent executions')).toHaveAttribute('aria-busy', 'true');
    expect(screen.queryByText('No recent background jobs found')).not.toBeInTheDocument();
  });

  it('explains a failed recent-jobs query without an inline retry control', () => {
    testState.recentJobsQuery = { data: undefined, isLoading: false, isError: true, refetch: vi.fn() };
    render(<BackgroundJobsView />);

    expect(screen.getByRole('alert')).toHaveTextContent('Unable to load recent executions.');
    expect(screen.queryByRole('button', { name: 'Reload recent executions' })).not.toBeInTheDocument();
  });
});
