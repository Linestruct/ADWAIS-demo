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
import { useOrgSelection } from './useOrgSelection';

export interface UserProfile {
  id: string;
  name: string;
  role: 'Admin' | 'Employee' | 'Viewer' | 'PlatformAdmin';
  organizationId?: string | null;
  organizationName?: string | null;
  tenantId?: string | null;
  isPlatformAdmin?: boolean;
}

export interface AccessScope {
  isAdmin: boolean;
  isPlatformAdmin: boolean;
  organizationId: string | null;
  organizationName: string | null;
  tenantId: string | null;
}

function isBackendUnavailableError(error: unknown): boolean {
  const status = (error as { status?: number } | null)?.status;
  if (status === 502 || status === 503 || status === 504) return true;

  const message = (error instanceof Error ? error.message : String(error)).toLowerCase();
  return message.includes('failed to fetch')
    || message.includes('network error')
    || message.includes('connection refused')
    || message.includes('load failed')
    || message.includes('bad gateway')
    || message.includes('service unavailable')
    || message.includes('gateway timeout');
}

const EMPTY_SCOPE: AccessScope = {
  isAdmin: false,
  isPlatformAdmin: false,
  organizationId: null,
  organizationName: null,
  tenantId: null,
};

function deriveScope(profile: UserProfile | null): AccessScope {
  if (!profile) return EMPTY_SCOPE;
  return {
    isAdmin: profile.role === 'Admin' || profile.role === 'PlatformAdmin',
    isPlatformAdmin: profile.isPlatformAdmin === true,
    organizationId: profile.organizationId ?? null,
    organizationName: profile.organizationName ?? null,
    tenantId: profile.tenantId ?? null,
  };
}

export function useCurrentUser() {
  const kioskToken = getKioskToken();
  const auth = useContext(AuthContext);
  const hasOidcUser = auth?.isAuthenticated === true;
  const isOidcLoading = auth?.isLoading === true;
  const { selectedOrgId } = useOrgSelection({ loadOrganizations: false });

  const kioskUser = kioskToken ? parseJwt(kioskToken) : null;
  const kioskRole = kioskUser?.role as 'Admin' | 'Employee' | 'Viewer' | 'PlatformAdmin' | undefined;
  const kioskOrganizationId = (kioskUser?.org_id as string | undefined) ?? null;
  const kioskIsPlatformAdmin = kioskUser?.is_platform_admin === 'true';

  const oidcQuery = useQuery<UserProfile>({
    queryKey: ['current-user', selectedOrgId],
    queryFn: () =>
      apiFetch<UserProfile>('/api/users/me', {
        headers: { 'X-Bypass-Global-401': 'true' },
      }),
    enabled: hasOidcUser,
    retry: false,
    retryOnMount: false,
    refetchOnMount: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  const kioskQuery = useQuery<UserProfile>({
    queryKey: ['current-user', 'kiosk'],
    queryFn: () => apiFetch<UserProfile>('/api/users/me'),
    enabled: !!kioskToken,
    retry: false,
    retryOnMount: false,
    refetchOnMount: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    staleTime: 5 * 60 * 1000,
  });

  // Calculate profile and role dynamically on render
  if (hasOidcUser) {
    const user = oidcQuery.data || null;
    const oidcError = oidcQuery.error as (Error & { status?: number }) | null;
    return {
      isLoading: isOidcLoading || oidcQuery.isLoading,
      user,
      role: user?.role || null,
      scope: deriveScope(user),
      isUnprovisioned: !oidcQuery.isLoading && user === null && (oidcError?.status === 401 || oidcError?.status === 403),
      isBackendUnavailable: !oidcQuery.isLoading && user === null && !!oidcError && isBackendUnavailableError(oidcError),
      isAccessCheckError: !oidcQuery.isLoading && user === null && !!oidcError
        && oidcError.status !== 401
        && oidcError.status !== 403
        && !isBackendUnavailableError(oidcError),
      retryAccessCheck: oidcQuery.refetch,
    };
  }

  if (kioskToken) {
    const serverProfile = kioskQuery.data;
    const user: UserProfile = {
      id: kioskUser?.sub || 'kiosk',
      name: (kioskUser?.name as string | undefined) || 'Kiosk Device',
      role: kioskRole || 'Viewer',
      organizationId: serverProfile?.organizationId ?? kioskOrganizationId,
      organizationName: serverProfile?.organizationName ?? null,
      tenantId: serverProfile?.tenantId ?? null,
      isPlatformAdmin: serverProfile?.isPlatformAdmin ?? kioskIsPlatformAdmin,
    };
    return {
      isLoading: kioskQuery.isLoading,
      user,
      role: kioskRole || 'Viewer',
      scope: deriveScope(user),
      isUnprovisioned: false,
      isBackendUnavailable: false,
      isAccessCheckError: false,
      retryAccessCheck: kioskQuery.refetch,
    };
  }

  return {
    isLoading: isOidcLoading,
    user: null,
    role: null,
    scope: EMPTY_SCOPE,
    isUnprovisioned: false,
    isBackendUnavailable: false,
    isAccessCheckError: false,
    retryAccessCheck: oidcQuery.refetch,
  };
}
