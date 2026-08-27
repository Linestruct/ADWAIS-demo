// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useOrgSelection } from './useOrgSelection';
import { ORG_SELECTION_KEY, ORG_SELECTION_RESET_EVENT } from '../utils/orgSelection';
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

describe('useOrgSelection', () => {
  beforeEach(() => {
    state.orgs = [{ id: 'org-1', name: 'Acme' }, { id: 'org-2', name: 'Beta' }];
    toast.info.mockClear();
  });

  it('initializes from session storage', () => {
    stubSessionStorage('org-2');

    const { result } = renderHook(() => useOrgSelection());

    expect(result.current.selectedOrgId).toBe('org-2');
  });

  it('persists a new selection and exposes the available organizations', () => {
    stubSessionStorage(null);

    const { result } = renderHook(() => useOrgSelection());

    act(() => result.current.setSelectedOrgId('org-1'));

    expect(sessionStorage.setItem).toHaveBeenCalledWith(ORG_SELECTION_KEY, 'org-1');
    expect(result.current.selectedOrgId).toBe('org-1');
    expect(result.current.organizations).toHaveLength(2);
  });

  it('clears the stored selection when switching to the platform overview', () => {
    stubSessionStorage('org-1');

    const { result } = renderHook(() => useOrgSelection());

    act(() => result.current.setSelectedOrgId(null));

    expect(sessionStorage.removeItem).toHaveBeenCalledWith(ORG_SELECTION_KEY);
    expect(result.current.selectedOrgId).toBeNull();
  });

  it('resets to null and notifies when a revoked selection is cleared', () => {
    stubSessionStorage('org-1');

    const { result } = renderHook(() => useOrgSelection());

    act(() => window.dispatchEvent(new Event(ORG_SELECTION_RESET_EVENT)));

    expect(result.current.selectedOrgId).toBeNull();
    expect(toast.info).toHaveBeenCalled();
  });
});