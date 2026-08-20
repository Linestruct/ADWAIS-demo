// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQuery } from '@tanstack/react-query';
import { useContext } from 'react';
import { AuthContext } from 'react-oidc-context';
import { getKioskToken } from '../utils/auth';
import { parseJwt } from '../utils/jwt';
import { apiFetch } from '../apiClient';

export interface UserProfile {
  id: string;
  name: string;
  role: 'Admin' | 'Employee' | 'Viewer';
}

export function useCurrentUser() {
  const kioskToken = getKioskToken();
  const auth = useContext(AuthContext);
  const hasOidcUser = auth?.isAuthenticated === true;

  const kioskUser = kioskToken ? parseJwt(kioskToken) : null;
  const kioskRole = kioskUser?.role as 'Admin' | 'Employee' | 'Viewer' | undefined;

  const oidcQuery = useQuery<UserProfile>({
    queryKey: ['current-user'],
    queryFn: () => apiFetch<UserProfile>('/api/users/me'),
    enabled: hasOidcUser,
    retry: false,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  // Calculate profile and role dynamically on render
  if (hasOidcUser) {
    return {
      isLoading: oidcQuery.isLoading,
      user: oidcQuery.data || null,
      role: oidcQuery.data?.role || null,
    };
  }

  if (kioskToken) {
    return {
      isLoading: false,
      user: {
        id: kioskUser?.sub || 'kiosk',
        name: kioskUser?.name || 'Kiosk Device',
        role: kioskRole || 'Viewer',
      } as UserProfile,
      role: kioskRole || 'Viewer',
    };
  }

  return {
    isLoading: false,
    user: null,
    role: null,
  };
}
