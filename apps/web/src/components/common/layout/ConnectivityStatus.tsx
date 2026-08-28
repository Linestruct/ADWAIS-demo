// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import {ServerCrash, WifiOff} from 'lucide-react';

type ConnectivityStatusProps = {
  isOnline: boolean;
  isBackendOnline: boolean;
};

export function ConnectivityStatus({isOnline, isBackendOnline}: ConnectivityStatusProps) {
  if (!isOnline) {
    return (
      <span
        className="flex h-11 items-center gap-2 rounded-full border border-red-200 bg-red-50 px-4 text-sm font-extrabold uppercase tracking-wider text-red-600 animate-in fade-in duration-300 whitespace-nowrap shrink-0"
        title="Application is offline"
      >
        <WifiOff size={16} className="animate-pulse" />
        <span>Offline</span>
      </span>
    );
  }

  if (!isBackendOnline) {
    return (
      <span
        className="flex h-11 items-center gap-2 rounded-full border border-amber-200 bg-amber-50 px-4 text-sm font-extrabold uppercase tracking-wider text-amber-600 animate-in fade-in duration-300 whitespace-nowrap shrink-0"
        title="Backend server is unreachable"
      >
        <ServerCrash size={16} className="animate-pulse" />
        <span>Server Offline</span>
      </span>
    );
  }

  return null;
}
