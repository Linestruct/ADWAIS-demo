// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute, Navigate, Link } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, KeyRound, LoaderCircle, MonitorUp, ServerCrash } from 'lucide-react';
import { setKioskToken } from '../utils/auth';
import { useKioskTokenQuery, type RegisterKioskResponse } from '../hooks/useKioskAuth';
import { customClient } from '../apiClient';

export const Route = createLazyFileRoute('/kiosk')({
  component: KioskLanding,
});

function KioskLanding() {
  const { deviceId } = Route.useLoaderData();

  const { data: registerData, failureCount } = useQuery({
    queryKey: ['kiosk', 'register', deviceId],
    queryFn: () => customClient<RegisterKioskResponse>({
      url: '/api/kiosk/register',
      method: 'POST',
      data: { deviceId },
    }),
    staleTime: Infinity,
    retry: Infinity,
    retryDelay: 5000,
  });

  const isServerUnreachable = failureCount > 0;
  const activationCode = registerData?.activationCode;
  const { data: tokenData } = useKioskTokenQuery(deviceId, !!activationCode);

  if (tokenData?.token) {
    setKioskToken(tokenData.token);
    return <Navigate to="/fleet-status" />;
  }

  if (activationCode) {
    return (
      <div className="relative z-10 flex flex-1 flex-col items-center justify-between gap-8">
        <div className="flex flex-col items-center gap-4 animate-stagger delay-100">
          <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary-container text-on-primary-container">
            <MonitorUp size={32} aria-hidden="true" />
          </div>
          <div className="text-center">
            <p className="mb-2 text-sm font-black uppercase tracking-[0.2em] text-brand-link">Display setup</p>
            <h1 className="text-3xl font-extrabold tracking-tight text-on-surface sm:text-4xl">Activate kiosk display</h1>
            <p className="mx-auto mt-3 max-w-lg text-base font-medium leading-relaxed text-on-surface-variant">
              Sign in as a staff member on another device, then enter this code under <strong>Settings → Authentication</strong>.
            </p>
          </div>
        </div>

        <div className="flex w-full flex-col items-center gap-5 animate-stagger delay-200">
          <div className="w-full rounded-3xl border border-outline-variant bg-surface-container px-5 py-6 sm:px-8">
            <div className="mb-4 flex items-center justify-center gap-2 text-sm font-black uppercase tracking-wide text-on-surface-variant">
              <KeyRound size={18} aria-hidden="true" /> Activation code
            </div>
            <div className="select-all overflow-hidden text-ellipsis whitespace-nowrap font-mono text-4xl font-black tracking-[0.16em] text-brand-link sm:text-6xl">
              {activationCode}
            </div>
          </div>

          <div className="inline-flex min-h-11 items-center gap-3 rounded-full bg-secondary-container px-5 text-sm font-bold text-on-secondary-container">
            <span className="relative flex h-3 w-3" aria-hidden="true">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-brand-link opacity-75" />
              <span className="relative inline-flex h-3 w-3 rounded-full bg-brand-link" />
            </span>
            Waiting for authorization
          </div>
        </div>

        <Link
          to="/login"
          className="inline-flex min-h-12 items-center gap-3 rounded-full border border-outline px-6 font-bold uppercase tracking-wide text-on-surface transition-colors hover:bg-surface-container"
        >
          <ArrowLeft size={18} aria-hidden="true" /> Staff login
        </Link>
      </div>
    );
  }

  if (isServerUnreachable) {
    return (
      <div className="relative z-10 flex flex-1 flex-col items-center justify-center gap-8 text-center">
        <div className="flex h-20 w-20 items-center justify-center rounded-3xl bg-error-container text-on-error-container">
          <ServerCrash size={38} aria-hidden="true" />
        </div>
        <div>
          <h1 className="text-3xl font-extrabold text-on-surface">Server unavailable</h1>
          <p className="mx-auto mt-3 max-w-sm text-base font-medium leading-relaxed text-on-surface-variant">
            ADWAIS cannot reach the backend. This display will retry automatically when connectivity returns.
          </p>
        </div>
        <div className="inline-flex min-h-11 items-center gap-3 rounded-full bg-surface-container px-5 text-sm font-bold text-on-surface-variant">
          <LoaderCircle size={18} className="animate-spin" aria-hidden="true" /> Retrying automatically
        </div>
        <Link to="/login" className="inline-flex min-h-12 items-center gap-3 rounded-full border border-outline px-6 font-bold uppercase tracking-wide text-on-surface transition-colors hover:bg-surface-container">
          <ArrowLeft size={18} aria-hidden="true" /> Staff login
        </Link>
      </div>
    );
  }

  return (
    <div className="relative z-10 flex flex-1 flex-col items-center justify-center gap-8 text-center">
      <div className="flex h-20 w-20 items-center justify-center rounded-3xl bg-primary-container text-on-primary-container">
        <LoaderCircle size={40} className="animate-spin" aria-hidden="true" />
      </div>
      <div>
        <h1 className="text-3xl font-extrabold text-on-surface">Preparing this display</h1>
        <p className="mt-3 text-base font-medium text-on-surface-variant">Generating a secure activation code…</p>
      </div>
    </div>
  );
}
