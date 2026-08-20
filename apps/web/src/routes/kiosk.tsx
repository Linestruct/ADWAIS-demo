// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createFileRoute, redirect } from '@tanstack/react-router';
import { type QueryClient } from '@tanstack/react-query';
import { getOrCreateDeviceId, getKioskToken } from '../utils/auth';
import { type RegisterKioskResponse } from '../hooks/useKioskAuth';
import { customClient } from '../apiClient';
import { userManager } from '../utils/oidcConfig';

export const Route = createFileRoute('/kiosk')({
  beforeLoad: async () => {
    const user = await userManager?.getUser();
    if (user && !user.expired) {
      throw redirect({ to: '/financial' });
    }
    if (getKioskToken()) {
      throw redirect({ to: '/fleet-status' });
    }
  },
  loader: ({ context }) => {
    const queryClient = (context as unknown as { queryClient: QueryClient }).queryClient;
    const deviceId = getOrCreateDeviceId();
    
    queryClient.prefetchQuery({
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
    return { deviceId };
  },
});
