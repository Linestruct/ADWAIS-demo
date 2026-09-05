// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {useEffect, useState} from 'react';
import {useQueryClient} from '@tanstack/react-query';

const isConnectivityError = (err: unknown): boolean => {
  if (!err) return false;
  const msg = (err instanceof Error ? err.message : String(err)).toLowerCase();

  return (
    msg.includes('502') ||
    msg.includes('bad gateway') ||
    msg.includes('failed to fetch') ||
    msg.includes('network error') ||
    msg.includes('504') ||
    msg.includes('gateway timeout') ||
    msg.includes('connection refused') ||
    msg.includes('load failed')
  );
};

export function useConnectivityStatus() {
  const queryClient = useQueryClient();
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [isBackendOnline, setIsBackendOnline] = useState(true);

  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  useEffect(() => {
    const checkCache = () => {
      const queries = queryClient.getQueryCache().getAll();
      const hasConnectionError = queries.some((query) => isConnectivityError(query.state.error));
      setIsBackendOnline(!hasConnectionError);
    };

    checkCache();

    const unsubscribe = queryClient.getQueryCache().subscribe((event) => {
      if (event.type === 'updated') {
        checkCache();
      }
    });

    return unsubscribe;
  }, [queryClient]);

  return {isOnline, isBackendOnline};
}
