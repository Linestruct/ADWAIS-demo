// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { test, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiFetch, getAuthHeaders } from './apiClient';
import { ORG_SELECTION_CHECK_EVENT } from './utils/orgSelection';
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

  const mockSessionStorage: Storage = {
    getItem: vi.fn().mockReturnValue(null),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
    length: 0,
    key: vi.fn(),
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

test('apiFetch attaches the stored organization as X-ADWAIS-ORG-ID', async () => {
  vi.mocked(sessionStorage.getItem).mockReturnValue('org-1');

  await apiFetch('http://test.local/api/tenants');

  const headers = vi.mocked(fetch).mock.calls[0][1]?.headers as Headers;
  expect(headers.get('X-ADWAIS-ORG-ID')).toBe('org-1');
});

test('apiFetch scopes the identity endpoint with the selected organization', async () => {
  vi.mocked(sessionStorage.getItem).mockReturnValue('org-1');

  await apiFetch('http://test.local/api/users/me');

  const headers = vi.mocked(fetch).mock.calls[0][1]?.headers as Headers;
  expect(headers.get('X-ADWAIS-ORG-ID')).toBe('org-1');
});

test('apiFetch never scopes the organization list endpoint', async () => {
  vi.mocked(sessionStorage.getItem).mockReturnValue('org-1');

  await apiFetch('http://test.local/api/organizations');

  const headers = vi.mocked(fetch).mock.calls[0][1]?.headers as Headers;
  expect(headers.get('X-ADWAIS-ORG-ID')).toBeNull();
});

test('apiFetch does not scope requests made with a kiosk token', async () => {
  vi.mocked(localStorage.getItem).mockReturnValue('kiosk-token');
  vi.mocked(sessionStorage.getItem).mockReturnValue('org-1');

  await apiFetch('http://test.local/api/tenants');

  const headers = vi.mocked(fetch).mock.calls[0][1]?.headers as Headers;
  expect(headers.get('X-ADWAIS-ORG-ID')).toBeNull();
});

test('apiFetch asks the app to re-validate the selection on 403 instead of clearing it', async () => {
  const checkListener = vi.fn();
  window.addEventListener(ORG_SELECTION_CHECK_EVENT, checkListener);
  vi.mocked(sessionStorage.getItem).mockReturnValue('org-1');

  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 403,
    text: async () => 'Forbidden',
  }));

  await expect(apiFetch('http://test.local/api/tenants')).rejects.toThrow();

  expect(sessionStorage.removeItem).not.toHaveBeenCalled();
  expect(checkListener).toHaveBeenCalledOnce();
  window.removeEventListener(ORG_SELECTION_CHECK_EVENT, checkListener);
});

test('apiFetch leaves the selection alone on 403 when no organization was selected', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 403,
    text: async () => 'Forbidden',
  }));

  await expect(apiFetch('http://test.local/api/tenants')).rejects.toThrow();

  expect(sessionStorage.removeItem).not.toHaveBeenCalled();
});

test('apiFetch does not redirect on 403 for /api/users/me when no OIDC user exists', async () => {
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

  expect(mockLocation.href).toBe('http://localhost/financial');
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

test('apiFetch does not log out on 403 for /api/users/me when an OIDC user exists', async () => {
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

  expect(mockSessionStorage.clear).not.toHaveBeenCalled();
  expect(userManager!.removeUser).not.toHaveBeenCalled();
  expect(mockLocation.href).toBe('http://localhost/financial');
});

test('apiFetch does not check session validity on 403 for non-profile routes', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });

  const fetchMock = vi.fn().mockResolvedValue({
    ok: false,
    status: 403,
    text: async () => 'Forbidden',
  });
  vi.stubGlobal('fetch', fetchMock);

  await expect(apiFetch('http://test.local/api/financial/summary')).rejects.toThrow();

  await new Promise((resolve) => setTimeout(resolve, 10));

  expect(fetchMock).not.toHaveBeenCalledWith('/api/users/me', expect.any(Object));
  expect(mockLocation.href).toBe('http://localhost/financial');
});

test('apiFetch does not redirect on 403 for non-profile routes even if session is stale', async () => {
  const mockLocation = {
    pathname: '/financial',
    href: 'http://localhost/financial',
  };
  vi.stubGlobal('window', { location: mockLocation });

  const fetchMock = vi.fn().mockResolvedValue({
    ok: false,
    status: 403,
    text: async () => 'Forbidden',
  });
  vi.stubGlobal('fetch', fetchMock);

  await expect(apiFetch('http://test.local/api/financial/summary')).rejects.toThrow();

  await new Promise((resolve) => setTimeout(resolve, 10));

  expect(fetchMock).not.toHaveBeenCalledWith('/api/users/me', expect.any(Object));
  expect(mockLocation.href).toBe('http://localhost/financial');
});

test('apiFetch reloads with a fresh kiosk token when the device is still authorized', async () => {
  const reload = vi.fn();
  const location = {
    pathname: '/fleet-status',
    href: 'http://localhost/fleet-status',
    reload,
  };
  vi.stubGlobal('window', { location });
  vi.mocked(localStorage.getItem).mockImplementation((key: string) =>
    key === 'kiosk_device_id' ? 'kiosk-1' : key === 'kiosk_token' ? 'expired-token' : null);
  vi.stubGlobal('fetch', vi.fn()
    .mockResolvedValueOnce({ ok: false, status: 401, text: async () => 'Unauthorized' })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ token: 'fresh-token', expiresInHours: 1 }) }));

  await expect(apiFetch('http://test.local/api/users/me')).rejects.toThrow();

  expect(localStorage.setItem).toHaveBeenCalledWith('kiosk_token', 'fresh-token');
  expect(reload).toHaveBeenCalledOnce();
  expect(location.href).toBe('http://localhost/fleet-status');
});

test('apiFetch falls through to /kiosk when the device is no longer authorized', async () => {
  const location = {
    pathname: '/fleet-status',
    href: 'http://localhost/fleet-status',
    reload: vi.fn(),
  };
  vi.stubGlobal('window', { location });
  vi.mocked(localStorage.getItem).mockImplementation((key: string) =>
    key === 'kiosk_device_id' ? 'kiosk-1' : key === 'kiosk_token' ? 'expired-token' : null);
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 401,
    text: async () => 'Unauthorized',
    json: async () => ({}),
  }));

  await expect(apiFetch('http://test.local/api/users/me')).rejects.toThrow();

  expect(location.href).toBe('/kiosk');
  expect(location.reload).not.toHaveBeenCalled();
});

