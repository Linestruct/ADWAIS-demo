// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createFileRoute, redirect } from '@tanstack/react-router';
import { fleetSearchSchema, type Timeframe } from '../schemas';
import { getSavedTimeframe } from '../utils/timeframeStorage';

export interface FleetSearch {
  timeframe?: Timeframe;
  tenantId?: string;
  monitorId?: number;
}

export const Route = createFileRoute('/fleet-status')({
  validateSearch: (search: Record<string, unknown>): FleetSearch => 
    fleetSearchSchema.parse(search) as FleetSearch,
  beforeLoad: ({ search }) => {
    if (!search.timeframe) {
      throw redirect({
        to: '/fleet-status',
        search: { ...search, timeframe: getSavedTimeframe('/fleet-status') },
        replace: true,
      });
    }
  },
});
