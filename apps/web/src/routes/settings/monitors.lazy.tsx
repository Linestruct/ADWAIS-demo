// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute } from '@tanstack/react-router';
import { MonitorsView } from '../../pages/Settings/monitors';

export const Route = createLazyFileRoute('/settings/monitors')({
  component: MonitorsView,
});
