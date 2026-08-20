// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useSearch, useNavigate } from '@tanstack/react-router';
import { useMemo, useCallback, useState } from 'react';
import { 
  useGlobalKpis, 
  useAccumulatedRevenue, 
  usePortfolioImpact, 
  useRevenueEfficiency,
  useCrossSegmentDistribution,
  useTransactionDensity
} from './useFinancialQueries';
import type { Timeframe } from '../schemas';
import type { TenantType, TransactionDensityPeriod } from '@types';
import { useTenantsQuery } from './useTenantQueries';

export function useFinancialViewModel() {
  const search = useSearch({ strict: false }) as { tenantId?: string, timeframe: Timeframe };
  const tenantId = search.tenantId;
  const timeframe = search.timeframe; // Re-hydrated by beforeLoad
  
  const navigate = useNavigate({ from: '/financial' });
  const [densityPeriod, setDensityPeriod] = useState<TransactionDensityPeriod>('Auto');
  const [selectedTenantTypes, setSelectedTenantTypesState] = useState<TenantType[]>([]);

  const tenantsQuery = useTenantsQuery();
  const kpiQuery = useGlobalKpis(timeframe, undefined, undefined, selectedTenantTypes);
  const velocityQuery = useAccumulatedRevenue(timeframe, undefined, 'YearOverYear', selectedTenantTypes);
  const portfolioImpactQuery = usePortfolioImpact(timeframe, 'YearOverYear', selectedTenantTypes);
  const efficiencyQuery = useRevenueEfficiency(timeframe, 'YearOverYear', selectedTenantTypes);
  const crossSegmentDistributionQuery = useCrossSegmentDistribution(timeframe, 'YearOverYear', selectedTenantTypes);
  const densityQuery = useTransactionDensity(densityPeriod, undefined, selectedTenantTypes);

  const tenantOptions = useMemo(() => (tenantsQuery.data ?? [])
    .filter(tenant => tenant.id && tenant.name)
    .map(tenant => ({
      id: tenant.id!,
      name: tenant.name!,
      type: tenant.type ?? 'Mixed',
    }))
    .sort((a, b) => a.name.localeCompare(b.name)), [tenantsQuery.data]);

  const selectedTenantDetails = useMemo(() => {
    const efficiencyTenants = efficiencyQuery.data?.tenants;
    const portfolioImpactTenants = portfolioImpactQuery.data?.tenants;
    const tenant = tenantOptions.find(option => option.id === tenantId);

    const tenantName = tenant?.name
      || efficiencyTenants?.find((d) => d.tenantId === tenantId)?.tenantName
      || portfolioImpactTenants?.find((t) => t.tenantId === tenantId)?.tenantName
      || 'Unknown Tenant';

    const type = tenant?.type
      || efficiencyTenants?.find((d) => d.tenantId === tenantId)?.type
      || portfolioImpactTenants?.find((t) => t.tenantId === tenantId)?.type
      || 'Mixed';
      
    return { tenantName, type };
  }, [tenantId, efficiencyQuery.data, portfolioImpactQuery.data, tenantOptions]);

  const handleTenantSelect = useCallback((id: string) => {
    void navigate({ search: (prev: Record<string, unknown>) => ({ ...prev, tenantId: id }) });
  }, [navigate]);

  const handleTenantChange = useCallback((id: string | null) => {
    void navigate({ search: (prev: Record<string, unknown>) => ({ ...prev, tenantId: id ?? undefined }) });
  }, [navigate]);

  const setSelectedTenantTypes = useCallback((types: TenantType[]) => {
    const selectedTenant = tenantOptions.find(option => option.id === tenantId);
    if (selectedTenant && types.length > 0 && !types.includes(selectedTenant.type)) return;
    setSelectedTenantTypesState(types);
  }, [tenantId, tenantOptions]);

  const clearFinancialFilters = useCallback(() => {
    setSelectedTenantTypesState([]);
    void navigate({ search: (prev: Record<string, unknown>) => ({ ...prev, tenantId: undefined }) });
  }, [navigate]);

  return {
    timeframe,
    tenantId,
    selectedTenantDetails,
    tenantsQuery,
    tenantOptions,
    selectedTenantTypes,
    setSelectedTenantTypes,
    efficiencyData: efficiencyQuery.data,
    portfolioImpactData: portfolioImpactQuery.data,
    crossSegmentDistributionData: crossSegmentDistributionQuery.data,
    kpiQuery,
    velocityQuery,
    portfolioImpactQuery,
    efficiencyQuery,
    crossSegmentDistributionQuery,
    densityQuery,
    densityPeriod,
    setDensityPeriod,
    handleTenantSelect,
    handleTenantChange,
    clearFinancialFilters
  };
}
