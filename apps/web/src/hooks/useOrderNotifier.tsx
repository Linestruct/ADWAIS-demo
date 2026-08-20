// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { getApiFinancialOrders, useGetApiFinancialOrders } from '../api/generated/endpoints';
import { useLocation } from '@tanstack/react-router';
import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import type { TenantResponseDto } from '@types';
import { useKiosk } from '../components/common/dashboard/KioskContext';
import { getTenantFaviconUrl } from '../utils/tenantHelper';
import { OrderToast } from '../components/common/ui/OrderToast';

export function useOrderNotifier() {
  const { pathname } = useLocation();
  const isSettings = pathname.startsWith('/settings');
  const queryClient = useQueryClient();
  const { notificationsEnabled } = useKiosk();

  const [lastCheckedTime, setLastCheckedTime] = useState<string>(() => new Date().toISOString());

  useGetApiFinancialOrders(
    { dateSince: lastCheckedTime },
    {
      query: {
        enabled: !isSettings,
        refetchInterval: 30000,
        staleTime: 0,
        queryFn: async ({ signal }) => {
          const res = await getApiFinancialOrders({ dateSince: lastCheckedTime }, { signal });
          
          if (res && res.data && res.data.length > 0) {
            const newOrders = res.data.filter(
              (order) => order.createdDate && order.createdDate > lastCheckedTime
            );

            if (newOrders.length > 0) {
              setTimeout(() => {
                queryClient.invalidateQueries({ queryKey: ['financial'] });
                queryClient.invalidateQueries({ queryKey: ['system-health'] });

                newOrders.forEach((order) => {
                  const displayValue = order.totalValueExcVat 
                    ? `${order.totalValueExcVat.toLocaleString()} ${order.currency || 'SEK'}`
                    : 'N/A';

                  const tenantsRes = queryClient.getQueryData<{ data: TenantResponseDto[] }>(['tenants']);
                  const tenantsList = tenantsRes?.data || [];
                  const tenant = tenantsList.find((t) => t.id === order.adwaisTenantId);
                  const faviconUrl = tenant ? getTenantFaviconUrl(tenant.orderProviderSettings?.endpointUrl) : null;

                  if (notificationsEnabled) {
                    toast.custom(
                      (t) => (
                        <OrderToast 
                          order={order} 
                          t={t} 
                          faviconUrl={faviconUrl} 
                          displayValue={displayValue} 
                        />
                      ),
                      {
                        duration: 10000,
                        position: 'bottom-left',
                      }
                    );
                  }
                });

                const newestOrder = newOrders[0];
                if (newestOrder.createdDate) {
                  setLastCheckedTime(newestOrder.createdDate);
                }
              }, 0);
            }
          }
          
          return res;
        }
      },
    }
  );
}
