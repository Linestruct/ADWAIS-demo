// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { useTenantsQuery } from './useTenantQueries';
import { useMonitorsQuery, useUnassignedMonitorsQuery } from './useMonitorQueries';
import { useFleetMonitors } from './useFleetQueries';
import { useGlobalKpis } from './useFinancialQueries';

vi.mock('./useOrgSelection', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./useOrgSelection')>();
  return {
    ...actual,
    useOrgSelection: () => ({
      selectedOrgId: state.selectedOrgId,
      setSelectedOrgId: vi.fn(),
      organizations: [],
    }),
  };
});

const state = vi.hoisted(() => ({ selectedOrgId: null as string | null }));

vi.mock('../api/generated/endpoints', async () => {
  const { useQuery } = await vi.importActual<typeof import('@tanstack/react-query')>('@tanstack/react-query');
  const hookFactory = () => (_params: unknown, options?: { query?: object }) =>
    useQuery({ queryKey: ['unset'], ...(options?.query ?? {}) });
  return {
    useGetApiTenants: hookFactory(),
    useGetApiMonitors: hookFactory(),
    useGetApiMonitorsUnassigned: hookFactory(),
    useGetApiMonitorsAnalytics: hookFactory(),
    useGetApiMonitorsAvailability: hookFactory(),
    useGetApiFinancialKpis: hookFactory(),
  };
});

function keysOf(queryClient: QueryClient) {
  return queryClient.getQueryCache().findAll().map(query => query.queryKey);
}

describe('org-scoped query keys', () => {
  beforeEach(() => {
    state.selectedOrgId = null;
  });

  it.each([
    ['tenants', () => useTenantsQuery()],
    ['monitors', () => useMonitorsQuery()],
    ['unassigned monitors', () => useUnassignedMonitorsQuery()],
    ['fleet monitors', () => useFleetMonitors('T30')],
    ['financial kpis', () => useGlobalKpis('T30')],
  ] as [string, () => ReturnType<typeof useTenantsQuery>][])(
    'keys %s by the selected organization and refetches on switch',
    (_label, useHook) => {
      const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
      const localWrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      );

      const { rerender } = renderHook(useHook, { wrapper: localWrapper });
      const firstKeys = keysOf(queryClient);
      expect(firstKeys.some(key => key.includes(null))).toBe(true);

      state.selectedOrgId = 'org-1';
      rerender();

      const secondKeys = keysOf(queryClient);
      expect(secondKeys.some(key => key.includes('org-1'))).toBe(true);
      expect(secondKeys).not.toEqual(firstKeys);
    },
  );
});