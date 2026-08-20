// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {ServerCrash, WifiOff} from 'lucide-react';

type ConnectivityStatusProps = {
  isOnline: boolean;
  isBackendOnline: boolean;
  variant: 'mobile' | 'desktop';
};

export function ConnectivityStatus({isOnline, isBackendOnline, variant}: ConnectivityStatusProps) {
  if (!isOnline) {
    return (
      <span
        className={variant === 'mobile'
          ? 'flex items-center gap-2 px-4 py-1.5 rounded-full text-sm font-extrabold text-red-600 bg-red-50 border border-red-200 uppercase tracking-wider animate-in fade-in duration-300 whitespace-nowrap shrink-0'
          : 'flex items-center gap-2 px-4 py-1.5 rounded-full text-sm font-extrabold text-red-600 bg-red-50 border border-red-200 uppercase tracking-wider animate-in fade-in duration-300 whitespace-nowrap shrink-0'}
        title="Application is offline"
      >
        <WifiOff size={variant === 'mobile' ? 12 : 14} className="animate-pulse" />
        <span>Offline</span>
      </span>
    );
  }

  if (!isBackendOnline) {
    return (
      <span
        className={variant === 'mobile'
          ? 'flex items-center gap-2 px-4 py-1.5 rounded-full text-sm font-extrabold text-amber-600 bg-amber-50 border border-amber-200 uppercase tracking-wider animate-in fade-in duration-300 whitespace-nowrap shrink-0'
          : 'flex items-center gap-2 px-4 py-1.5 rounded-full text-sm font-extrabold text-amber-600 bg-amber-50 border border-amber-200 uppercase tracking-wider animate-in fade-in duration-300 whitespace-nowrap shrink-0'}
        title="Backend server is unreachable"
      >
        <ServerCrash size={variant === 'mobile' ? 12 : 14} className="animate-pulse" />
        <span>{variant === 'mobile' ? 'Server' : 'Server Offline'}</span>
      </span>
    );
  }

  return null;
}
