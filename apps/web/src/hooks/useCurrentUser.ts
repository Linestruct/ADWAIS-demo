// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
  role: 'Admin' | 'Employee' | 'Viewer';
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
    isAdmin: profile.role === 'Admin',
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
  const { selectedOrgId } = useOrgSelection();

  const kioskUser = kioskToken ? parseJwt(kioskToken) : null;
  const kioskRole = kioskUser?.role as 'Admin' | 'Employee' | 'Viewer' | undefined;
  const kioskOrganizationId = (kioskUser?.org_id as string | undefined) ?? null;
  const kioskIsPlatformAdmin = kioskUser?.is_platform_admin === 'true';

  const oidcQuery = useQuery<UserProfile>({
    queryKey: ['current-user', selectedOrgId],
    queryFn: () => apiFetch<UserProfile>('/api/users/me'),
    enabled: hasOidcUser,
    retry: false,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  const kioskQuery = useQuery<UserProfile>({
    queryKey: ['current-user', 'kiosk'],
    queryFn: () => apiFetch<UserProfile>('/api/users/me'),
    enabled: !!kioskToken,
    retry: false,
  });

  // Calculate profile and role dynamically on render
  if (hasOidcUser) {
    const user = oidcQuery.data || null;
    return {
      isLoading: oidcQuery.isLoading,
      user,
      role: user?.role || null,
      scope: deriveScope(user),
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
    };
  }

  return {
    isLoading: false,
    user: null,
    role: null,
    scope: EMPTY_SCOPE,
  };
}