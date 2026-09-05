// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute } from '@tanstack/react-router';
import { UserDetailView } from '../../pages/Settings/UserDetail';
import { RoleBoundary } from '../../components/common/ui/RoleBoundary';

export const Route = createLazyFileRoute('/settings/users_/$userId')({
  component: () => (
    <RoleBoundary requiredRole="Viewer">
      <UserDetailView />
    </RoleBoundary>
  ),
});
