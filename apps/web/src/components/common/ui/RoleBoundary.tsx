// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { type ReactNode } from 'react';
import { useCurrentUser } from '../../../hooks/useCurrentUser';
import { AccessDeniedCard } from './AccessDeniedCard';

interface Props {
  requiredRole: 'Admin' | 'Employee' | 'Viewer';
  children: ReactNode;
  fallback?: ReactNode;
}

export function RoleBoundary({ requiredRole, children, fallback }: Props) {
  const { role, isLoading } = useCurrentUser();

  if (isLoading) {
    return null;
  }

  const rolePrecedence: Record<string, number> = {
    'Admin': 3,
    'Employee': 2,
    'Viewer': 1
  };

  const userPrecedence = role ? (rolePrecedence[role] ?? 0) : 0;
  const requiredPrecedence = rolePrecedence[requiredRole] ?? 0;

  if (userPrecedence < requiredPrecedence) {
    return <>{fallback ?? <AccessDeniedCard message={`Requires ${requiredRole} role.`} />}</>;
  }

  return <>{children}</>;
}
