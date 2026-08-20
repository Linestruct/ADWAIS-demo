// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SystemEventsView } from './events';

const testState = vi.hoisted(() => ({
  viewModel: {} as Record<string, unknown>,
}));

vi.mock('../../hooks/useSystemEventsViewModel', () => ({
  useSystemEventsViewModel: () => testState.viewModel,
}));

vi.mock('../../api/generated/endpoints', () => ({
  usePostApiDashboardSession: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

function baseViewModel() {
  return {
    isAdmin: true,
    health: undefined,
    events: undefined,
    isLoadingHealth: false,
    isHealthError: false,
    isLoadingEvents: false,
    isEventsError: false,
    clearErrorsMutation: { mutate: vi.fn(), isPending: false },
  };
}

describe('system events state', () => {
  beforeEach(() => {
    testState.viewModel = baseViewModel();
  });

  it('shows console loading rows instead of an empty event history', () => {
    testState.viewModel = { ...baseViewModel(), isLoadingEvents: true };
    render(<SystemEventsView />);

    expect(screen.getByLabelText('Loading system events')).toHaveAttribute('aria-busy', 'true');
    expect(screen.queryByText('No system events found.')).not.toBeInTheDocument();
  });

  it('explains query failures without adding route-level reload controls', () => {
    testState.viewModel = {
      ...baseViewModel(),
      isHealthError: true,
      isEventsError: true,
    };
    render(<SystemEventsView />);

    expect(screen.getByText(/Unable to load pipeline health/)).toBeVisible();
    expect(screen.getByText(/Unable to load system events/)).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Reload pipeline health' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Reload system events' })).not.toBeInTheDocument();
  });
});
