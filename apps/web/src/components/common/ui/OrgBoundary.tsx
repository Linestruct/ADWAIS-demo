// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { type ReactNode } from 'react';
import { useCurrentUser } from '../../../hooks/useCurrentUser';
import { AccessDeniedCard } from './AccessDeniedCard';

interface Props {
  organizationId: string;
  children: ReactNode;
  fallback?: ReactNode;
}

export function OrgBoundary({ organizationId, children, fallback }: Props) {
  const { scope, isLoading } = useCurrentUser();

  if (isLoading) {
    return null;
  }

  const inScope = scope.isPlatformAdmin || scope.organizationId === organizationId;
  if (!inScope) {
    return <>{fallback ?? <AccessDeniedCard message="You do not have access to this organization." />}</>;
  }

  return <>{children}</>;
}