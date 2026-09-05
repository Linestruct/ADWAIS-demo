// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { useContext, useState } from 'react';
import { AuthContext } from 'react-oidc-context';
import { getApiDemoToken } from '../api/generated/endpoints';
import { removeKioskToken, setKioskToken } from '../utils/auth';
import { isDemoMode, isOidcConfigured, ssoButtonLabel, ssoButtonLogoUrl } from '../utils/oidcConfig';

export const Route = createLazyFileRoute('/login')({
  component: LoginComponent,
});

function LoginComponent() {
  const auth = useContext(AuthContext);
  const navigate = useNavigate();
  const [isDemoSigningIn, setIsDemoSigningIn] = useState(false);
  const [demoError, setDemoError] = useState<string | null>(null);

  const handleLogin = () => {
    removeKioskToken();
    auth?.signinRedirect().catch((err) => {
      console.error('OIDC Login error:', err);
    });
  };

  const handleDemoLogin = async () => {
    setIsDemoSigningIn(true);
    setDemoError(null);
    try {
      const { data } = await getApiDemoToken();
      const token = (data as { token?: string }).token;
      if (!token) throw new Error('Demo token response did not contain a token.');
      setKioskToken(token);
      await navigate({ to: '/fleet-status' });
    } catch (error) {
      console.error('Demo login error:', error);
      setDemoError('Demo access is currently unavailable.');
    } finally {
      setIsDemoSigningIn(false);
    }
  };

  return (
    <>
      <div className="flex flex-col items-center animate-stagger delay-100">
        <h1 className="font-display text-7xl font-extrabold text-brand-text tracking-tight relative z-10">ADWAIS</h1>
        <h2 className="font-mono text-2xl font-semibold mb-3 text-on-tertiary-container tracking-wide relative z-10"><i>/ədˈvaɪs/</i></h2>
      </div>

      <div className="w-full relative z-10 flex flex-col items-center divide-y divide-outline animate-stagger delay-200">
        <div className="flex w-full flex-col items-center gap-3 pt-12 pb-6">
          <div className="flex w-full flex-wrap items-center justify-center gap-3">
            {isOidcConfigured && (
              <button
                onClick={handleLogin}
                className="py-4 px-6 bg-surface-container-highest hover:bg-brand-btn-primary-hover font-extrabold rounded-full transition-all duration-300 m3-elevation-1 hover:m3-elevation-2 cursor-pointer inline-flex items-center justify-center gap-3 group text-base tracking-wide uppercase whitespace-nowrap"
              >
                {ssoButtonLogoUrl && (
                  <img
                    src={ssoButtonLogoUrl}
                    className="w-5 h-5 shrink-0"
                    alt=""
                    aria-hidden="true"
                  />
                )}
                <span>{ssoButtonLabel}</span>
              </button>
            )}

            {isDemoMode && (
              <button
                onClick={handleDemoLogin}
                disabled={isDemoSigningIn}
                className="py-4 px-6 bg-primary-container hover:bg-primary-container/80 font-extrabold rounded-full transition-all duration-300 m3-elevation-1 hover:m3-elevation-2 cursor-pointer disabled:cursor-wait disabled:opacity-60 inline-flex items-center justify-center gap-3 text-base tracking-wide uppercase whitespace-nowrap"
              >
                {isDemoSigningIn ? 'Starting demo…' : 'Continue as demo user'}
              </button>
            )}
          </div>
          {demoError && <p className="text-sm font-semibold text-error" role="alert">{demoError}</p>}
        </div>

        <div className="flex w-full justify-center pt-6">
          <Link
            to="/kiosk"
            className="inline-flex min-h-12 items-center justify-center rounded-full border border-on-surface-variant px-6 py-3 text-base font-bold uppercase tracking-wide transition-colors hover:bg-white/50"
          >
            Activate kiosk
          </Link>
        </div>
      </div>

      <div className="pt-4 flex justify-between items-center text-sm text-on-surface-variant tracking-wide uppercase relative z-10 w-full animate-stagger delay-300">
        <span><a href={"https://marmenlind.com?utm_source=adwais"} target="_blank" rel="noopener noreferrer">© Marmenlind</a></span>
        <span className="text-on-surface-variant">v1.0.0</span>
      </div>
    </>
  );
}
