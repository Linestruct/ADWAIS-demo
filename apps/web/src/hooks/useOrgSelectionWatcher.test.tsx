// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { act, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { useOrgSelectionWatcher } from './useOrgSelectionWatcher';
import { ORG_SELECTION_CHECK_EVENT, getSelectedOrgId, setSelectedOrgId } from '../utils/orgSelection';
import type { OrganizationResponseDto } from '@types';

const toast = vi.hoisted(() => ({ info: vi.fn(), success: vi.fn(), error: vi.fn() }));
vi.mock('sonner', () => ({ toast }));

const state = vi.hoisted(() => ({ orgs: [] as OrganizationResponseDto[] }));
vi.mock('./useMembershipQueries', () => ({
  useOrganizationsForPickerQuery: () => ({ data: state.orgs }),
}));

function wrapper(queryClient: QueryClient) {
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useOrgSelectionWatcher', () => {
  beforeEach(() => {
    state.orgs = [{ id: 'org-1', name: 'Acme' }, { id: 'org-2', name: 'Beta' }];
    setSelectedOrgId(null);
    toast.info.mockClear();
  });

  it('keeps a selection that is still reachable', () => {
    setSelectedOrgId('org-1');

    renderHook(() => useOrgSelectionWatcher(), { wrapper: wrapper(new QueryClient()) });

    expect(getSelectedOrgId()).toBe('org-1');
    expect(toast.info).not.toHaveBeenCalled();
  });

  it('resets a selection that is not reachable once the list settles', async () => {
    setSelectedOrgId('org-1');
    state.orgs = [{ id: 'org-2', name: 'Beta' }];

    renderHook(() => useOrgSelectionWatcher(), { wrapper: wrapper(new QueryClient()) });

    await waitFor(() => expect(getSelectedOrgId()).toBeNull());
    expect(toast.info).toHaveBeenCalled();
  });

  it('does not reset while the reachable list is still loading', () => {
    setSelectedOrgId('org-1');
    state.orgs = [];

    renderHook(() => useOrgSelectionWatcher(), { wrapper: wrapper(new QueryClient()) });

    expect(getSelectedOrgId()).toBe('org-1');
    expect(toast.info).not.toHaveBeenCalled();
  });

  it('invalidates the reachable list when a request is denied', async () => {
    setSelectedOrgId('org-1');
    const queryClient = new QueryClient();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    renderHook(() => useOrgSelectionWatcher(), { wrapper: wrapper(queryClient) });

    act(() => {
      window.dispatchEvent(new Event(ORG_SELECTION_CHECK_EVENT));
    });

    await waitFor(() =>
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['organizations'] }),
    );
  });
});