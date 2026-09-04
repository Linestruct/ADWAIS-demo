// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

export const KIOSK_DEVICE_ID_KEY = 'kiosk_device_id';
export const KIOSK_TOKEN_KEY = 'kiosk_token';

function generateUUID(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

export function getOrCreateDeviceId(): string {
  let deviceId = localStorage.getItem(KIOSK_DEVICE_ID_KEY);
  if (!deviceId) {
    deviceId = `kiosk-${generateUUID()}`;
    localStorage.setItem(KIOSK_DEVICE_ID_KEY, deviceId);
    document.cookie = `${KIOSK_DEVICE_ID_KEY}=${deviceId}; path=/; max-age=31536000; samesite=strict`;
  }
  return deviceId;
}

export function getStoredDeviceId(): string | null {
  return localStorage.getItem(KIOSK_DEVICE_ID_KEY);
}

export function getKioskToken(): string | null {
  return localStorage.getItem(KIOSK_TOKEN_KEY);
}

export function setKioskToken(token: string) {
  localStorage.setItem(KIOSK_TOKEN_KEY, token);
}

export function removeKioskToken() {
  localStorage.removeItem(KIOSK_TOKEN_KEY);
}

const KIOSK_REFRESH_ATTEMPT_KEY = 'kiosk_refresh_attempt';
const KIOSK_REFRESH_LOOP_GUARD_MS = 5 * 60 * 1000;

/**
 * Fetches a fresh token for the stored device id without touching the
 * session. Returns true when a new token was stored. At most one attempt
 * per guard window, so a display that keeps failing cannot reload-loop.
 * Uses plain fetch, not apiFetch: this runs inside session invalidation,
 * where apiFetch's global 401 handling would recurse into itself.
 */
export async function tryRefreshKioskToken(): Promise<boolean> {
  const deviceId = getStoredDeviceId();
  if (!deviceId) return false;
  const lastAttempt = Number(sessionStorage.getItem(KIOSK_REFRESH_ATTEMPT_KEY) ?? 0);
  if (Date.now() - lastAttempt < KIOSK_REFRESH_LOOP_GUARD_MS) return false;
  sessionStorage.setItem(KIOSK_REFRESH_ATTEMPT_KEY, String(Date.now()));
  try {
    const response = await fetch(`/api/kiosk/token?deviceId=${encodeURIComponent(deviceId)}`);
    if (!response.ok) return false;
    const data = (await response.json()) as { token?: string };
    if (!data.token) return false;
    setKioskToken(data.token);
    return true;
  } catch {
    return false;
  }
}
