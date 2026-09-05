// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ORG_SELECTION_KEY } from '../utils/orgSelection';
import type { OrganizationResponseDto } from '@types';

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
    vi.resetModules();
  });

  it('initializes from session storage', async () => {
    stubSessionStorage('org-2');
    const { useOrgSelection } = await import('./useOrgSelection');

    const { result } = renderHook(() => useOrgSelection());

    expect(result.current.selectedOrgId).toBe('org-2');
    expect(result.current.organizations).toHaveLength(2);
  });

  it('propagates a switch to every mounted instance', async () => {
    stubSessionStorage('org-1');
    const { useOrgSelection } = await import('./useOrgSelection');

    const first = renderHook(() => useOrgSelection());
    const second = renderHook(() => useOrgSelection());
    expect(first.result.current.selectedOrgId).toBe('org-1');
    expect(second.result.current.selectedOrgId).toBe('org-1');

    act(() => first.result.current.setSelectedOrgId('org-2'));

    expect(sessionStorage.setItem).toHaveBeenCalledWith(ORG_SELECTION_KEY, 'org-2');
    expect(first.result.current.selectedOrgId).toBe('org-2');
    expect(second.result.current.selectedOrgId).toBe('org-2');
  });

  it('clears the stored selection when switching to the platform overview', async () => {
    stubSessionStorage('org-1');
    const { useOrgSelection } = await import('./useOrgSelection');

    const { result } = renderHook(() => useOrgSelection());

    act(() => result.current.setSelectedOrgId(null));

    expect(sessionStorage.removeItem).toHaveBeenCalledWith(ORG_SELECTION_KEY);
    expect(result.current.selectedOrgId).toBeNull();
  });
});