// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { test, expect, vi, beforeEach, afterEach } from 'vitest';
import { getStoredDeviceId, tryRefreshKioskToken } from './auth';

beforeEach(() => {
  const store = new Map<string, string>();
  const mockLocalStorage: Storage = {
    getItem: vi.fn((key: string) => store.get(key) ?? null),
    setItem: vi.fn((key: string, value: string) => void store.set(key, value)),
    removeItem: vi.fn((key: string) => void store.delete(key)),
    clear: vi.fn(() => store.clear()),
    length: 0,
    key: vi.fn(),
  };
  vi.stubGlobal('localStorage', mockLocalStorage);

  const sessionStore = new Map<string, string>();
  const mockSessionStorage: Storage = {
    getItem: vi.fn((key: string) => sessionStore.get(key) ?? null),
    setItem: vi.fn((key: string, value: string) => void sessionStore.set(key, value)),
    removeItem: vi.fn((key: string) => void sessionStore.delete(key)),
    clear: vi.fn(() => sessionStore.clear()),
    length: 0,
    key: vi.fn(),
  };
  vi.stubGlobal('sessionStorage', mockSessionStorage);
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

test('getStoredDeviceId returns null without creating one', () => {
  expect(getStoredDeviceId()).toBeNull();
  expect(localStorage.getItem).toHaveBeenCalledWith('kiosk_device_id');
  expect(localStorage.setItem).not.toHaveBeenCalled();
});

test('tryRefreshKioskToken stores a fresh token for a known device', async () => {
  localStorage.setItem('kiosk_device_id', 'kiosk-1');
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: true,
    json: async () => ({ token: 'fresh-token', expiresInHours: 1 }),
  }));

  await expect(tryRefreshKioskToken()).resolves.toBe(true);
  expect(localStorage.getItem('kiosk_token')).toBe('fresh-token');
  expect(vi.mocked(fetch)).toHaveBeenCalledWith('/api/kiosk/token?deviceId=kiosk-1');
});

test('tryRefreshKioskToken skips without a stored device id', async () => {
  const fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);

  await expect(tryRefreshKioskToken()).resolves.toBe(false);
  expect(fetchMock).not.toHaveBeenCalled();
});

test('tryRefreshKioskToken leaves the stored token on failure', async () => {
  localStorage.setItem('kiosk_device_id', 'kiosk-1');
  localStorage.setItem('kiosk_token', 'old-token');
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 401 }));

  await expect(tryRefreshKioskToken()).resolves.toBe(false);
  expect(localStorage.getItem('kiosk_token')).toBe('old-token');
});

test('tryRefreshKioskToken attempts only once per guard window', async () => {
  localStorage.setItem('kiosk_device_id', 'kiosk-1');
  const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 401 });
  vi.stubGlobal('fetch', fetchMock);

  await expect(tryRefreshKioskToken()).resolves.toBe(false);
  await expect(tryRefreshKioskToken()).resolves.toBe(false);
  expect(fetchMock).toHaveBeenCalledTimes(1);
});
