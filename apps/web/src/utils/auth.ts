// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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

export function getKioskToken(): string | null {
  return localStorage.getItem(KIOSK_TOKEN_KEY);
}

export function setKioskToken(token: string) {
  localStorage.setItem(KIOSK_TOKEN_KEY, token);
}

export function removeKioskToken() {
  localStorage.removeItem(KIOSK_TOKEN_KEY);
}
