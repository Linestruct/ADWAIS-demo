// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SyncStatusWidget } from './SyncStatusWidget';

const state = vi.hoisted(() => ({
  isPlatformAdmin: false,
  kioskToken: null as string | null,
  queryCalls: [] as Array<{ enabled?: boolean }>,
}));

vi.mock('../../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ user: { isPlatformAdmin: state.isPlatformAdmin } }),
}));

vi.mock('@tanstack/react-router', () => ({
  useSearch: () => ({}),
  useParams: () => ({}),
  useRouterState: ({ select }: { select: (state: { matches: Array<{ routeId: string; pathname: string }> }) => unknown }) =>
    select({ matches: [{ routeId: '/financial', pathname: '/financial' }] }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: (options: { enabled?: boolean }) => {
    state.queryCalls.push(options);
    return { data: undefined, isLoading: false };
  },
  useQueryClient: () => ({
    getQueryCache: () => ({
      findAll: () => [],
      subscribe: () => () => {},
    }),
    invalidateQueries: vi.fn(),
  }),
  useIsFetching: () => 0,
}));

vi.mock('../../../apiClient', () => ({ apiFetch: vi.fn() }));
vi.mock('../../../utils/auth', () => ({
  getKioskToken: () => state.kioskToken,
}));

describe('SyncStatusWidget', () => {
  beforeEach(() => {
    state.isPlatformAdmin = false;
    state.kioskToken = null;
    state.queryCalls = [];
  });

  it('does not enable the platform health request for organization users', () => {
    render(<SyncStatusWidget />);

    expect(state.queryCalls[0]?.enabled).toBe(false);
  });

  it('enables the platform health request for platform administrators', () => {
    state.isPlatformAdmin = true;

    render(<SyncStatusWidget />);

    expect(state.queryCalls[0]?.enabled).toBe(true);
  });

  it('does not enable the platform health request for kiosk tokens', () => {
    state.isPlatformAdmin = true;
    state.kioskToken = 'kiosk-token';

    render(<SyncStatusWidget />);

    expect(state.queryCalls[0]?.enabled).toBe(false);
  });
});
