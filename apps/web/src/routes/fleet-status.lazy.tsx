// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute } from '@tanstack/react-router';
import { FleetStatus } from '../pages/FleetStatus';

export const Route = createLazyFileRoute('/fleet-status')({
  component: FleetStatus,
});
