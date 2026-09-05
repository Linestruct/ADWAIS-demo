// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { type ReactNode } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

import { useCurrentUser, type UserProfile } from './useCurrentUser';

const mockAuthState = vi.hoisted(() => ({ isAuthenticated: true }));

vi.mock('react-oidc-context', async () => {
  const { createContext } = await vi.importActual<typeof import('react')>('react');
  return {
    AuthContext: createContext(mockAuthState),
  };
});

const mockKioskToken = vi.hoisted(() => vi.fn());
vi.mock('../utils/auth', () => ({
  getKioskToken: () => mockKioskToken(),
}));

const mockParseJwt = vi.hoisted(() => vi.fn());
vi.mock('../utils/jwt', () => ({
  parseJwt: (token: string) => mockParseJwt(token),
}));

const mockApiFetch = vi.hoisted(() => vi.fn());
vi.mock('../apiClient', () => ({
  apiFetch: (...args: unknown[]) => mockApiFetch(...args),
}));

const selectionState = vi.hoisted(() => ({ selectedOrgId: null as string | null }));
vi.mock('./useOrgSelection', () => ({
  useOrgSelection: () => ({
    selectedOrgId: selectionState.selectedOrgId,
    setSelectedOrgId: vi.fn(),
    organizations: [],
  }),
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useCurrentUser', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockAuthState.isAuthenticated = true;
    mockKioskToken.mockReturnValue(null);
    selectionState.selectedOrgId = null;
  });

  it('derives an org scope from the profile for OIDC users', async () => {
    const profile: UserProfile = {
      id: 'u1',
      name: 'Alice',
      role: 'Admin',
      organizationId: 'org-1',
      organizationName: 'Acme',
      tenantId: null,
      isPlatformAdmin: false,
    };
    mockApiFetch.mockResolvedValue(profile);

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.scope.organizationId).toBe('org-1'));
    expect(result.current.scope).toEqual({
      isAdmin: true,
      isPlatformAdmin: false,
      organizationId: 'org-1',
      organizationName: 'Acme',
      tenantId: null,
    });
  });

  it('marks platform admins without an organization', async () => {
    mockApiFetch.mockResolvedValue({
      id: 'u2',
      name: 'Root',
      role: 'Admin',
      isPlatformAdmin: true,
    } satisfies UserProfile);

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.scope.isPlatformAdmin).toBe(true));
    expect(result.current.scope.organizationId).toBeNull();
  });

  it('refetches the identity when the selected organization changes', async () => {
    mockApiFetch.mockResolvedValue({
      id: 'u3',
      name: 'Switcher',
      role: 'Viewer',
      organizationId: 'org-1',
      organizationName: 'Acme',
      isPlatformAdmin: false,
    } satisfies UserProfile);

    const { result, rerender } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.scope.organizationId).toBe('org-1'));
    expect(mockApiFetch).toHaveBeenCalledTimes(1);

    selectionState.selectedOrgId = 'org-2';
    mockApiFetch.mockResolvedValue({
      id: 'u3',
      name: 'Switcher',
      role: 'Admin',
      organizationId: 'org-2',
      organizationName: 'Beta',
      isPlatformAdmin: false,
    } satisfies UserProfile);
    rerender();

    await waitFor(() => expect(result.current.scope.organizationId).toBe('org-2'));
    expect(result.current.scope.isAdmin).toBe(true);
  });

  it('derives the kiosk scope from token claims', () => {
    mockAuthState.isAuthenticated = false;
    mockKioskToken.mockReturnValue('kiosk-token');
    mockParseJwt.mockReturnValue({
      sub: 'device-7',
      name: 'Hall Display',
      role: 'Viewer',
      org_id: 'org-2',
    });

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    expect(result.current.user).toMatchObject({ id: 'device-7', role: 'Viewer' });
    expect(result.current.scope).toMatchObject({
      organizationId: 'org-2',
      isPlatformAdmin: false,
    });
  });

  it('resolves the kiosk organization name from the identity endpoint when present', async () => {
    mockAuthState.isAuthenticated = false;
    mockKioskToken.mockReturnValue('kiosk-token');
    mockParseJwt.mockReturnValue({
      sub: 'device-7',
      name: 'Hall Display',
      role: 'Viewer',
      org_id: 'org-2',
    });
    mockApiFetch.mockResolvedValue({
      id: 'device-7',
      name: 'Hall Display',
      role: 'Viewer',
      organizationId: 'org-2',
      organizationName: 'Beta',
      tenantId: null,
      isPlatformAdmin: false,
    } satisfies UserProfile);

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.scope.organizationName).toBe('Beta'));
    expect(result.current.user).toMatchObject({ organizationName: 'Beta', tenantId: null });
  });

  it('returns an empty scope when signed out', () => {

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    expect(result.current.scope).toEqual({
      isAdmin: false,
      isPlatformAdmin: false,
      organizationId: null,
      organizationName: null,
      tenantId: null,
    });
  });

  it('flags an authenticated user the identity endpoint rejects as unprovisioned', async () => {
    mockApiFetch.mockRejectedValue(Object.assign(new Error('User context is invalid.'), { status: 401 }));

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.isUnprovisioned).toBe(true));
    expect(result.current.user).toBeNull();
  });

  it('sends the session-preserving bypass header on the identity query', async () => {
    mockApiFetch.mockResolvedValue({
      id: 'u4',
      name: 'Cara',
      role: 'Viewer',
      isPlatformAdmin: false,
    } satisfies UserProfile);

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.user).not.toBeNull());
    expect(mockApiFetch).toHaveBeenCalledWith(
      '/api/users/me',
      expect.objectContaining({
        headers: expect.objectContaining({ 'X-Bypass-Global-401': 'true' }),
      }),
    );
  });

  it('does not flag non-401 identity failures as unprovisioned', async () => {
    mockApiFetch.mockRejectedValue(Object.assign(new Error('Server error.'), { status: 500 }));

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.isUnprovisioned).toBe(false));
    expect(result.current.user).toBeNull();
  });
});