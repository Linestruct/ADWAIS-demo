// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
  apiFetch: (url: string) => mockApiFetch(url),
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
    mockKioskToken.mockReturnValue(null);
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
});