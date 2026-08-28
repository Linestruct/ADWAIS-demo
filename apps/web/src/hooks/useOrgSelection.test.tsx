// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { act, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { useOrgSelection } from './useOrgSelection';
import { ORG_SELECTION_KEY } from '../utils/orgSelection';
import type { OrganizationResponseDto } from '@types';

const toast = vi.hoisted(() => ({ info: vi.fn(), success: vi.fn(), error: vi.fn() }));
vi.mock('sonner', () => ({ toast }));

const state = vi.hoisted(() => ({ orgs: [] as OrganizationResponseDto[] }));
vi.mock('./useMembershipQueries', () => ({
  useOrganizationsForPickerQuery: () => ({ data: state.orgs }),
}));

function stubSessionStorage(stored: string | null) {
  const store: Record<string, string> = stored === null ? {} : { [ORG_SELECTION_KEY]: stored };
  vi.stubGlobal('sessionStorage', {
    getItem: vi.fn((key: string) => store[key] ?? null),
    setItem: vi.fn((key: string, value: string) => { store[key] = value; }),
    removeItem: vi.fn((key: string) => { delete store[key]; }),
    clear: vi.fn(() => { for (const key of Object.keys(store)) delete store[key]; }),
    length: 0,
    key: vi.fn(),
  });
}

function createWrapper(queryClient: QueryClient) {
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useOrgSelection', () => {
  beforeEach(() => {
    state.orgs = [{ id: 'org-1', name: 'Acme' }, { id: 'org-2', name: 'Beta' }];
    toast.info.mockClear();
  });

  it('initializes from session storage', () => {
    stubSessionStorage('org-2');

    const { result } = renderHook(() => useOrgSelection(), { wrapper: createWrapper(new QueryClient()) });

    expect(result.current.selectedOrgId).toBe('org-2');
  });

  it('persists a new selection and exposes the available organizations', () => {
    stubSessionStorage(null);

    const { result } = renderHook(() => useOrgSelection(), { wrapper: createWrapper(new QueryClient()) });

    act(() => result.current.setSelectedOrgId('org-1'));

    expect(sessionStorage.setItem).toHaveBeenCalledWith(ORG_SELECTION_KEY, 'org-1');
    expect(result.current.selectedOrgId).toBe('org-1');
    expect(result.current.organizations).toHaveLength(2);
  });

  it('clears the stored selection when switching to the platform overview', () => {
    stubSessionStorage('org-1');

    const { result } = renderHook(() => useOrgSelection(), { wrapper: createWrapper(new QueryClient()) });

    act(() => result.current.setSelectedOrgId(null));

    expect(sessionStorage.removeItem).toHaveBeenCalledWith(ORG_SELECTION_KEY);
    expect(result.current.selectedOrgId).toBeNull();
  });

  it('keeps a selection that is still reachable', () => {
    stubSessionStorage('org-1');

    const { result } = renderHook(() => useOrgSelection(), { wrapper: createWrapper(new QueryClient()) });

    expect(result.current.selectedOrgId).toBe('org-1');
    expect(sessionStorage.removeItem).not.toHaveBeenCalled();
    expect(toast.info).not.toHaveBeenCalled();
  });

  it('resets a selection that is not reachable once the list settles', async () => {
    stubSessionStorage('org-1');
    state.orgs = [{ id: 'org-2', name: 'Beta' }];

    const { result } = renderHook(() => useOrgSelection(), { wrapper: createWrapper(new QueryClient()) });

    await waitFor(() => expect(result.current.selectedOrgId).toBeNull());
    expect(sessionStorage.removeItem).toHaveBeenCalledWith(ORG_SELECTION_KEY);
    expect(toast.info).toHaveBeenCalled();
  });

  it('does not reset while the reachable list is still loading', () => {
    stubSessionStorage('org-1');
    state.orgs = [];

    const { result } = renderHook(() => useOrgSelection(), { wrapper: createWrapper(new QueryClient()) });

    expect(result.current.selectedOrgId).toBe('org-1');
  });
});