// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { formatCurrency, formatNumber } from '@utils';
import { FactPanel } from '../components/common/dashboard/FactPanel';
import { CumulativeGrowthDeltaChart } from '../components/TenantSpecific/CumulativeGrowthDeltaChart';
// import { NetGrowthAdditionChart } from '../components/TenantSpecific/NetGrowthAdditionChart';
import { OrderValueDistributionChart } from '../components/TenantSpecific/OrderValueDistributionChart';
import { AccumulatedRevenueChart } from '../components/financial/AccumulatedRevenueChart';
import { TransactionDensityChart } from '../components/financial/TransactionDensityChart';
import {
  useGlobalKpis, 
  useAccumulatedRevenue,
  useCumulativeGrowthDelta,
  // useNetGrowthAddition,
  useOrderDistribution,
  useTransactionDensity
} from '../hooks/useFinancialQueries';
import {DashboardLayout} from "../components/common/layout/DashboardLayout.tsx";
import {DashboardTopRow} from "../components/common/layout/DashboardTopRow.tsx";
import {DashboardFlexRow} from "../components/common/layout/DashboardFlexRow.tsx";
import { DashboardFooter } from "../components/common/layout/DashboardFooter.tsx";
import { SyncStatusWidget } from '../components/common/dashboard/SyncStatusWidget';
import { PeriodSelector } from '../components/common/charts/PeriodSelector';
import type { TenantType, TransactionDensityPeriod, TransactionDensityResponseDto } from '@types';
import { useState } from 'react';
import {
  FinancialFilterMenu,
  FinancialFilterPanel,
  type FinancialTenantOption,
} from '../components/financial/FinancialFilterMenu.tsx';
import { MobileFooterActions } from '../components/common/ui/MobileFooterActions.tsx';
import { countActiveFilterGroups } from '../utils/filterCounts.ts';
import { useNavigate } from '@tanstack/react-router';
import { Settings } from 'lucide-react';
import { Button } from '../components/common/ui/Button';

const EMPTY_ACCUMULATED: never[] = [];
const EMPTY_DENSITY: TransactionDensityResponseDto = {
  points: [],
  totalCount: 0,
  minCount: 0,
  maxCount: 0,
  averageCountPerBucket: 0,
  sampleQuality: 'Sparse',
  requestedPeriod: 'Auto',
  effectivePeriod: 'T30',
  timeZoneId: 'Europe/Stockholm',
  periodStart: '',
  periodEnd: '',
};
const EMPTY_DELTA: never[] = [];
// const EMPTY_NET_GROWTH: never[] = [];
const EMPTY_BINS: never[] = [];

interface Props {
  tenantId: string;
  tenantName: string;
  tenantType: TenantType;
  timeframe: string;
  tenantOptions: FinancialTenantOption[];
  selectedTenantTypes: TenantType[];
  tenantsLoading: boolean;
  onTenantChange: (tenantId: string | null) => void;
  onTypesChange: (types: TenantType[]) => void;
  onClearFilters: () => void;
}

export function TenantDiagnostics({
  tenantId,
  tenantName,
  tenantType,
  timeframe,
  tenantOptions,
  selectedTenantTypes,
  tenantsLoading,
  onTenantChange,
  onTypesChange,
  onClearFilters,
}: Props) {
  const navigate = useNavigate({ from: '/financial' });
  const [densityPeriod, setDensityPeriod] = useState<TransactionDensityPeriod>('Auto');
  const kpiQuery = useGlobalKpis(timeframe, tenantId);
  const globalKpiQuery = useGlobalKpis(timeframe, undefined, undefined, selectedTenantTypes);
  const accumulatedQuery = useAccumulatedRevenue(timeframe, tenantId, 'YearOverYear');
  const densityQuery = useTransactionDensity(densityPeriod, tenantId);
  const deltaQuery = useCumulativeGrowthDelta(timeframe, tenantId, 'YearOverYear');
  // const netGrowthQuery = useNetGrowthAddition(timeframe, tenantId);
  const orderQuery = useOrderDistribution(timeframe, tenantId);

  const kpis = kpiQuery.data;
  const globalKpis = globalKpiQuery.data;
  
  const shareOfRevenue = kpis && globalKpis && globalKpis.currentRevenue > 0 
    ? (kpis.currentRevenue / globalKpis.currentRevenue) * 100 
    : undefined;

  const shareOfRevenueDesc = selectedTenantTypes.length > 0
    ? `of ${selectedTenantTypes.join(', ')} Portfolio`
    : 'of Global Portfolio';

  return (
    <DashboardLayout>
      <header className="flex w-full shrink-0 flex-wrap items-center justify-between gap-2">
        <div className="min-w-0">
          <div className="flex items-center gap-6 mb-1">
            <h1 className="text-2xl font-extrabold text-brand-text tracking-tight m-0">{tenantName} Diagnostics</h1>
            <span 
              className={`inline-flex items-center px-4 py-1 rounded-2xl text-base font-black uppercase tracking-wide shadow-sm shrink-0 ${
                tenantType === 'B2C' ? 'bg-chart-1 text-white' : 
                tenantType === 'Mixed' ? 'bg-chart-2 text-white' :
                tenantType === 'B2B' ? 'bg-chart-3 text-white' :    
                'bg-tertiary-container text-white'
              }`}
            >
              {tenantType}
            </span>
          </div>
          <p className="text-sm text-on-surface-variant m-0 font-medium tracking-wide">Isolated entity performance view for the {timeframe} period. VAT included.</p>
        </div>
      </header>

      <DashboardTopRow>
        <FactPanel
          label={`Revenue (${timeframe})`}
          value={kpis ? formatCurrency(kpis.currentRevenue) : '\u2014'}
          isLoading={kpiQuery.isLoading}
          isError={kpiQuery.isError}
          extra={kpis?.revenueGrowthPercentage !== undefined
            ? { type: 'PoP', value: kpis.revenueGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
        <FactPanel
          label="Share of Revenue"
          value={shareOfRevenue !== undefined ? `${shareOfRevenue.toFixed(1)}%` : '\u2014'}
          isLoading={kpiQuery.isLoading || globalKpiQuery.isLoading}
          isError={kpiQuery.isError || globalKpiQuery.isError}
          extra={{ type: 'Desc', value: shareOfRevenueDesc }}
          hasExtra={true}
        />
        <FactPanel
          label="Transaction Volume"
          value={kpis ? formatNumber(kpis.transactionVolume) : '\u2014'}
          isLoading={kpiQuery.isLoading}
          isError={kpiQuery.isError}
          extra={kpis?.volumeGrowthPercentage !== undefined
            ? { type: 'PoP', value: kpis.volumeGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
        <FactPanel
          label="Average Order Value"
          value={kpis ? formatCurrency(kpis.averageOrderValue) : '\u2014'}
          isLoading={kpiQuery.isLoading}
          isError={kpiQuery.isError}
          extra={kpis?.aovGrowthPercentage !== undefined
            ? { type: 'PoP', value: kpis.aovGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
      </DashboardTopRow>

      <DashboardFlexRow weight={"flex-1"} gridCols={"2"}>
        <AccumulatedRevenueChart 
          points={accumulatedQuery.data || EMPTY_ACCUMULATED} 
          isLoading={accumulatedQuery.isLoading} 
          isError={accumulatedQuery.isError}
          isStale={accumulatedQuery.isPlaceholderData}
          comparison="YearOverYear"
          className="h-full min-h-[350px] contained:min-h-0"
        />
        
        <TransactionDensityChart
          response={densityQuery.data || EMPTY_DENSITY}
          selectedPeriod={densityPeriod}
          onPeriodChange={setDensityPeriod}
          isLoading={densityQuery.isLoading}
          isError={densityQuery.isError}
          isStale={densityQuery.isPlaceholderData}
          className="h-full min-h-[350px] contained:min-h-0"
        />

        <CumulativeGrowthDeltaChart
          points={deltaQuery.data || EMPTY_DELTA}
          isLoading={deltaQuery.isLoading}
          isError={deltaQuery.isError}
          isStale={deltaQuery.isPlaceholderData}
          comparison="YearOverYear"
          className="h-full min-h-[350px] contained:min-h-0"
        />

        <OrderValueDistributionChart 
          bins={orderQuery.data || EMPTY_BINS} 
          isLoading={orderQuery.isLoading} 
          isError={orderQuery.isError}
          isStale={orderQuery.isPlaceholderData}
          className="h-full min-h-[350px] contained:min-h-0"
        />
      </DashboardFlexRow>

      {/* ── Footer Widgets (All Resolutions) ── */}
      <DashboardFooter>
        <SyncStatusWidget />
        <div className="flex shrink-0 flex-wrap items-center justify-end gap-2">
          <Button
            variant="filled"
            color="surface"
            icon={<Settings size={18} />}
            aria-label={`Open settings for ${tenantName}`}
            title={`Open settings for ${tenantName}`}
            onClick={() => void navigate({ to: '/settings/tenants/$tenantId', params: { tenantId } })}
            className="!min-h-14 m3-elevation-1"
          >
            Edit tenant
          </Button>
          <FinancialFilterMenu
            tenants={tenantOptions}
            selectedTenantId={tenantId}
            selectedTypes={selectedTenantTypes}
            isLoading={tenantsLoading}
            onTenantChange={onTenantChange}
            onTypesChange={onTypesChange}
            onClearAll={onClearFilters}
          />
          <div className="mx-1 h-6 w-px shrink-0 bg-outline-variant" aria-hidden="true" />
          <PeriodSelector from="/financial" />
        </div>
      </DashboardFooter>

      <MobileFooterActions
        activeCount={countActiveFilterGroups(Boolean(tenantId), selectedTenantTypes.length > 0)}
        clearLabel="Clear all financial filters"
        onClearAll={onClearFilters}
        settingsAction={{
          label: `Edit ${tenantName}`,
          onClick: () => void navigate({ to: '/settings/tenants/$tenantId', params: { tenantId } }),
        }}
      >
        <FinancialFilterPanel
          embedded
          tenants={tenantOptions}
          selectedTenantId={tenantId}
          selectedTypes={selectedTenantTypes}
          isLoading={tenantsLoading}
          onTenantChange={onTenantChange}
          onTypesChange={onTypesChange}
          onClearAll={onClearFilters}
        />
      </MobileFooterActions>
    </DashboardLayout>
  );
}
