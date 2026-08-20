// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { afterEach, expect, test, vi } from 'vitest';

afterEach(() => {
  vi.unstubAllEnvs();
  vi.resetModules();
});

test('requires OIDC configuration outside demo mode', async () => {
  vi.stubEnv('VITE_DEMO_MODE', 'false');
  vi.stubEnv('VITE_OIDC_AUTHORITY', '');
  vi.stubEnv('VITE_OIDC_CLIENT_ID', '');
  vi.stubEnv('VITE_OIDC_SCOPE', '');

  await expect(import('./oidcConfig')).rejects.toThrow('OIDC configuration missing');
});

test('allows demo mode without OIDC configuration', async () => {
  vi.stubEnv('VITE_DEMO_MODE', 'true');
  vi.stubEnv('VITE_OIDC_AUTHORITY', '');
  vi.stubEnv('VITE_OIDC_CLIENT_ID', '');
  vi.stubEnv('VITE_OIDC_SCOPE', '');

  const config = await import('./oidcConfig');

  expect(config.isDemoMode).toBe(true);
  expect(config.userManager).toBeNull();
  expect(config.oidcConfig.scope).toBe('openid profile email');
});
