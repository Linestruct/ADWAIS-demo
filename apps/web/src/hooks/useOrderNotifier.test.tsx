// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

import { useOrderNotifier } from './useOrderNotifier';

const toastCustom = vi.hoisted(() => vi.fn());
vi.mock('sonner', () => ({
  toast: { custom: (...args: unknown[]) => toastCustom(...args) },
}));

const capturedQueryFn = vi.hoisted(() => ({ current: null as null | ((opts: { signal: unknown }) => Promise<unknown>) }));
vi.mock('../api/generated/endpoints', () => ({
  getApiFinancialOrders: vi.fn(),
  useGetApiFinancialOrders: (_params: unknown, opts: { query: { queryFn: (args: { signal: unknown }) => Promise<unknown> } }) => {
    capturedQueryFn.current = opts.query.queryFn;
    return { data: undefined };
  },
}));

vi.mock('@tanstack/react-router', () => ({
  useLocation: () => ({ pathname: '/' }),
}));

vi.mock('../components/common/dashboard/KioskContext', () => ({
  useKiosk: () => ({ notificationsEnabled: true }),
}));

vi.mock('../utils/tenantHelper', () => ({
  getTenantFaviconUrl: () => null,
}));

vi.mock('../components/common/ui/OrderToast', () => ({
  OrderToast: () => null,
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useOrderNotifier', () => {
  it('renders the order card in a bare wrapper without the global toast chrome', async () => {
    renderHook(() => useOrderNotifier(), { wrapper: createWrapper() });

    const { getApiFinancialOrders } = await import('../api/generated/endpoints') as unknown as {
      getApiFinancialOrders: ReturnType<typeof vi.fn>;
    };
    getApiFinancialOrders.mockResolvedValue({
      data: [
        {
          id: 'order-1',
          createdDate: new Date().toISOString(),
          adwaisTenantId: 'tenant-1',
          totalValueExcVat: 100,
          currency: 'SEK',
        },
      ],
    });

    await capturedQueryFn.current!({ signal: undefined });

    await waitFor(() => expect(toastCustom).toHaveBeenCalled());
    const options = toastCustom.mock.calls[0][1] as { classNames?: Record<string, string> };
    expect(options.classNames?.toast).toContain('order-toast-reset');
    expect(options.classNames?.closeButton).toContain('hidden');
  });
});
