// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { QueryClient, QueryCache } from '@tanstack/react-query';
import { toast } from 'sonner';

function isConnectivityError(error: unknown): boolean {
  const message = (error instanceof Error ? error.message : String(error)).toLowerCase();
  return message.includes('502')
    || message.includes('bad gateway')
    || message.includes('503')
    || message.includes('service unavailable')
    || message.includes('504')
    || message.includes('gateway timeout')
    || message.includes('failed to fetch')
    || message.includes('network error')
    || message.includes('connection refused')
    || message.includes('load failed');
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      const queryKey = query.queryKey[0];
      if (queryKey === 'kiosk') return; // Suppress toasts for background kiosk polling
      if (queryKey === 'current-user') return; // The shell renders the access-pending panel instead

      if (isConnectivityError(error)) {
        toast.error('Backend unavailable', {
          id: 'backend-unavailable',
          description: 'The server is not reachable right now.',
        });
        return;
      }

      let title = 'Failed to load data';
      if (typeof queryKey === 'string') {
        if (queryKey === 'global-config') title = 'Failed to load global configuration';
        else if (queryKey === 'tenants') title = 'Failed to load tenants';
        else if (queryKey === 'monitors') title = 'Failed to load monitors';
        else if (queryKey === 'unassigned-monitors') title = 'Failed to load unassigned monitors';
        else if (queryKey === 'job-recurring') title = 'Failed to load background jobs';
        else if (queryKey === 'users') title = 'Failed to load users';
        else if (queryKey === 'system-health') title = 'Failed to load pipeline health';
        else if (queryKey === 'system-events') title = 'Failed to load system events';
        else if (queryKey === 'system-jobs') title = 'Failed to load background job status';
      }
      toast.error(title, {
        description: error.message || String(error)
      });
    }
  }),
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => !isConnectivityError(error) && failureCount < 3,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});
