// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const authority = import.meta.env.VITE_OIDC_AUTHORITY;
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID;

export const isDemoMode = import.meta.env.VITE_DEMO_MODE === 'true';
export const isOidcConfigured = !!authority && !!clientId;
export const ssoButtonLabel = import.meta.env.VITE_SSO_BUTTON_LABEL || 'Sign in with SSO';
export const ssoButtonLogoUrl = import.meta.env.VITE_SSO_BUTTON_LOGO_URL || undefined;

if (!isDemoMode && !isOidcConfigured) {
  throw new Error(
    'ADWAIS: OIDC configuration missing. Set VITE_OIDC_AUTHORITY and VITE_OIDC_CLIENT_ID, or enable VITE_DEMO_MODE=true.',
  );
}

export const oidcConfig = {
  authority: authority ?? '',
  client_id: clientId ?? '',
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  scope: import.meta.env.VITE_OIDC_SCOPE || 'openid profile email',
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: sessionStorage }),
};

export const userManager = isOidcConfigured
  ? new UserManager(oidcConfig)
  : null;
