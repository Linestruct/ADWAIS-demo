// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute } from '@tanstack/react-router';
import { UsersView } from '../../pages/Settings/users';
import { RoleBoundary } from '../../components/common/ui/RoleBoundary';

export const Route = createLazyFileRoute('/settings/users')({
  component: () => (
    <RoleBoundary requiredRole="Viewer">
      <UsersView />
    </RoleBoundary>
  ),
});
