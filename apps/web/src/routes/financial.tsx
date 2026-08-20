// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createFileRoute, redirect } from '@tanstack/react-router';
import { financialSearchSchema, type Timeframe } from '../schemas';
import { getSavedTimeframe } from '../utils/timeframeStorage';

export interface FinancialSearch {
  timeframe?: Timeframe;
  tenantId?: string;
}

export const Route = createFileRoute('/financial')({
  validateSearch: (search: Record<string, unknown>): FinancialSearch => 
    financialSearchSchema.parse(search) as FinancialSearch,
  beforeLoad: ({ search }) => {
    if (!search.timeframe) {
      throw redirect({
        to: '/financial',
        search: { ...search, timeframe: getSavedTimeframe('/financial') },
        replace: true,
      });
    }
  },
});
