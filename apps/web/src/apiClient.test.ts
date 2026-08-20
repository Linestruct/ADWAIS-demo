// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { test, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiFetch, getAuthHeaders } from './apiClient';
import { userManager } from './utils/oidcConfig';
import type { User } from 'oidc-client-ts';

const oidcMock = vi.hoisted(() => ({ isDemoMode: false }));

vi.mock('./utils/oidcConfig', () => ({
  get isDemoMode() {
    return oidcMock.isDemoMode;
  },
  userManager: {
    getUser: vi.fn(),
    removeUser: vi.fn(),
  },
}));

beforeEach(() => {
  oidcMock.isDemoMode = false;
  const mockLocalStorage: Storage = {
    getItem: vi.fn().mockReturnValue(null),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
    length: 0,
    key: vi.fn(),
  };
  vi.stubGlobal('localStorage', mockLocalStorage);

  const mockSessionStorage = {
    clear: vi.fn(),
  };
  vi.stubGlobal('sessionStorage', mockSessionStorage);

  vi.mocked(userManager!.getUser).mockResolvedValue(null);
  vi.mocked(userManager!.removeUser).mockResolvedValue();

  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: true,
    text: async () => JSON.stringify({ success: true }),
  }));
});


afterEach(() => {
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

test('apiFetch attaches OIDC token when no kiosk token exists', async () => {
  const user = {
    access_token: 'oidc-token-123',
    expired: false,
  } as User;
  vi.mocked(userManager!.getUser).mockResolvedValue(user);

  await apiFetch('http://test.local');

  expect(userManager!.getUser).toHaveBeenCalled();
  const fetchCall = vi.mocked(fetch).mock.calls[0];
  const headers = fetchCall[1]?.headers as Headers;
  expect(headers.get('Authorization')).toBe('Bearer oidc-token-123');
});

test('apiFetch does not redirect to /kiosk on 401 when on /login', async () => {
  const mockLocation = {
    pathname: '/login',
    href: 'http://localhost/login',
  };
  vi.stubGlobal('window', { location: mockLocation });

  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 401,
    text: async () => 'Unauthorized',
  }));

  await expect(apiFetch('http://test.local')).rejects.toThrow();

  expect(mockLocation.href).toBe('http://localhost/login');
});

test('apiFetch redirects to /kiosk on 401 when on non-bypass route', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });

  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 401,
    text: async () => 'Unauthorized',
  }));

  await expect(apiFetch('http://test.local')).rejects.toThrow();

  expect(mockLocation.href).toBe('/kiosk');
});

test('apiFetch redirects to /kiosk on 403 for /api/users/me when no OIDC user exists', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });

  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 403,
    text: async () => 'Forbidden',
  }));

  await expect(apiFetch('http://test.local/api/users/me')).rejects.toThrow();

  expect(mockLocation.href).toBe('/kiosk');
});

test('OIDC token takes precedence over a stale kiosk token', async () => {
  const user = {
    access_token: 'oidc-admin-token',
    expired: false,
  } as User;
  vi.mocked(userManager!.getUser).mockResolvedValue(user);
  vi.mocked(localStorage.getItem).mockReturnValue('stale-demo-token');

  const headers = await getAuthHeaders();

  expect(headers.get('Authorization')).toBe('Bearer oidc-admin-token');
});

test('apiFetch reloads demo mode when its token is invalid', async () => {
  oidcMock.isDemoMode = true;
  const reload = vi.fn();
  vi.stubGlobal('window', {
    location: {
      pathname: '/financial',
      href: 'http://localhost/financial',
      reload,
    },
  });
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 401,
    text: async () => 'Unauthorized',
  }));

  await expect(apiFetch('http://test.local/api/users/me')).rejects.toThrow();

  expect(localStorage.removeItem).toHaveBeenCalledWith('kiosk_token');
  expect(reload).toHaveBeenCalledOnce();
});

test('apiFetch redirects to /login on 403 for /api/users/me when an OIDC user exists', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });
  
  vi.mocked(userManager!.getUser).mockResolvedValue({
    access_token: 'stale-token',
    expired: false,
  } as User);

  const mockSessionStorage = {
    clear: vi.fn(),
  };
  vi.stubGlobal('sessionStorage', mockSessionStorage);

  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 403,
    text: async () => 'Forbidden',
  }));

  await expect(apiFetch('http://test.local/api/users/me')).rejects.toThrow();

  expect(mockSessionStorage.clear).toHaveBeenCalled();
  expect(userManager!.removeUser).toHaveBeenCalled();
  expect(mockLocation.href).toBe('/login');
});

test('apiFetch does not redirect on 403 for non-profile routes if session is still valid', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });

  const fetchMock = vi.fn().mockImplementation((url: string) => {
    if (url.includes('/api/users/me')) {
      return Promise.resolve({
        ok: true,
        status: 200,
        text: async () => JSON.stringify({ role: 'Employee' }),
      });
    }
    return Promise.resolve({
      ok: false,
      status: 403,
      text: async () => 'Forbidden',
    });
  });
  vi.stubGlobal('fetch', fetchMock);

  await expect(apiFetch('http://test.local/api/financial/summary')).rejects.toThrow();

  await new Promise((resolve) => setTimeout(resolve, 10));

  expect(fetchMock).toHaveBeenCalledWith('/api/users/me', expect.any(Object));
  expect(mockLocation.href).toBe('http://localhost/financial');
});

test('apiFetch redirects on 403 for non-profile routes if session is stale/unauthorized', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });

  const fetchMock = vi.fn().mockImplementation((url: string) => {
    if (url.includes('/api/users/me')) {
      return Promise.resolve({
        ok: false,
        status: 403,
        text: async () => 'Forbidden',
      });
    }
    return Promise.resolve({
      ok: false,
      status: 403,
      text: async () => 'Forbidden',
    });
  });
  vi.stubGlobal('fetch', fetchMock);

  await expect(apiFetch('http://test.local/api/financial/summary')).rejects.toThrow();

  await new Promise((resolve) => setTimeout(resolve, 10));

  expect(fetchMock).toHaveBeenCalledWith('/api/users/me', expect.any(Object));
  expect(mockLocation.href).toBe('/kiosk');
});


