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

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: 'PlatformAdmin', user: { isPlatformAdmin: true } }),
}));

vi.mock('../../hooks/useOrgSelection', () => ({
  useOrgSelection: () => ({ selectedOrgId: null }),
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
});
