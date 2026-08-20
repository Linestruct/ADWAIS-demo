// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { formatCurrency, formatCompact, formatNumber } from '@utils';
import { FactPanel } from '../components/common/dashboard/FactPanel';
import { PortfolioImpactMatrixChart } from '../components/financial/PortfolioImpactMatrixChart';
import { CrossSegmentDistributionChart } from '../components/financial/CrossSegmentDistributionChart';
import { TransactionDensityChart } from '../components/financial/TransactionDensityChart';
import { AccumulatedRevenueChart } from '../components/financial/AccumulatedRevenueChart';
import { TenantDiagnostics } from './TenantDiagnostics';
import { DashboardLayout } from "../components/common/layout/DashboardLayout.tsx";
import { DashboardTopRow } from "../components/common/layout/DashboardTopRow.tsx";
import { DashboardFlexRow } from "../components/common/layout/DashboardFlexRow.tsx";
import { DashboardFooter } from "../components/common/layout/DashboardFooter.tsx";
import { SyncStatusWidget } from '../components/common/dashboard/SyncStatusWidget';
import { PeriodSelector } from '../components/common/charts/PeriodSelector';
import { useFinancialViewModel } from "../hooks/useFinancialViewModel.ts";
import type { PortfolioImpactResponse, TransactionDensityResponseDto, CrossSegmentDistributionResponse } from '@types';
import { FinancialFilterMenu, FinancialFilterPanel } from '../components/financial/FinancialFilterMenu.tsx';
import { MobileFooterActions } from '../components/common/ui/MobileFooterActions.tsx';
import { countActiveFilterGroups } from '../utils/filterCounts.ts';
import { useNavigate } from '@tanstack/react-router';
import { Settings } from 'lucide-react';
import { Button } from '../components/common/ui/Button';

const EMPTY_VELOCITY: never[] = [];
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
const EMPTY_PORTFOLIO_IMPACT: PortfolioImpactResponse = { tenants: [], medianBaselineRevenue: 0, globalGrowthPercentage: 0, medianPortfolioShare: 0 };
const EMPTY_CROSS_SEGMENT_DISTRIBUTION: CrossSegmentDistributionResponse = { cohorts: [], tenants: [] };

export function Financial() {
  const vm = useFinancialViewModel();
  const navigate = useNavigate({ from: '/financial' });

  if (vm.tenantId && vm.selectedTenantDetails) {
    return (
      <TenantDiagnostics
        tenantId={vm.tenantId}
        tenantName={vm.selectedTenantDetails.tenantName}
        tenantType={vm.selectedTenantDetails.type}
        timeframe={vm.timeframe}
        tenantOptions={vm.tenantOptions}
        selectedTenantTypes={vm.selectedTenantTypes}
        tenantsLoading={vm.tenantsQuery.isLoading}
        onTenantChange={vm.handleTenantChange}
        onTypesChange={vm.setSelectedTenantTypes}
        onClearFilters={vm.clearFinancialFilters}
      />
    );
  }

  const revenueLabel = vm.selectedTenantTypes.length > 0
    ? `${vm.selectedTenantTypes.join(', ')} Revenue (${vm.timeframe})`
    : `Global Revenue (${vm.timeframe})`;

  return (
    <DashboardLayout>
      {/* KPI Section */}
      <DashboardTopRow>
        <FactPanel
          label={revenueLabel}
          value={vm.kpiQuery.data ? formatCurrency(vm.kpiQuery.data.currentRevenue) : '\u2014'}
          isLoading={vm.kpiQuery.isLoading}
          isError={vm.kpiQuery.isError}
          extra={vm.kpiQuery.data?.revenueGrowthPercentage !== undefined
            ? { type: 'PoP', value: vm.kpiQuery.data.revenueGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
        <FactPanel
          label="Transaction Volume"
          value={vm.kpiQuery.data ? formatNumber(vm.kpiQuery.data.transactionVolume) : '\u2014'}
          isLoading={vm.kpiQuery.isLoading}
          isError={vm.kpiQuery.isError}
          extra={vm.kpiQuery.data?.volumeGrowthPercentage !== undefined
            ? { type: 'PoP', value: vm.kpiQuery.data.volumeGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
        <FactPanel
          label="Portfolio AOV"
          value={vm.kpiQuery.data ? `${formatCompact(vm.kpiQuery.data.averageOrderValue)} SEK` : '\u2014'}
          isLoading={vm.kpiQuery.isLoading}
          isError={vm.kpiQuery.isError}
          extra={vm.kpiQuery.data?.aovGrowthPercentage !== undefined
            ? { type: 'PoP', value: vm.kpiQuery.data.aovGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
        <FactPanel
          label="Active Tenants"
          value={vm.kpiQuery.data?.activeTenants !== undefined ? vm.kpiQuery.data.activeTenants.toString() : '\u2014'}
          isLoading={vm.kpiQuery.isLoading}
          isError={vm.kpiQuery.isError}
          extra={vm.kpiQuery.data?.activeTenantsGrowthPercentage !== undefined
            ? { type: 'PoP', value: vm.kpiQuery.data.activeTenantsGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
        <FactPanel
          label="Avg Revenue Per Tenant"
          value={vm.kpiQuery.data?.averageRevenuePerTenant !== undefined ? formatCompact(vm.kpiQuery.data.averageRevenuePerTenant) : '\u2014'}
          isLoading={vm.kpiQuery.isLoading}
          isError={vm.kpiQuery.isError}
          extra={vm.kpiQuery.data?.arptGrowthPercentage !== undefined
            ? { type: 'PoP', value: vm.kpiQuery.data.arptGrowthPercentage }
            : undefined}
          hasExtra={true}
        />
      </DashboardTopRow>

      {/* Charts Grid: Strictly Responsive & Independent */}
      <DashboardFlexRow weight={"flex-1"} gridCols={"2"}>
        <AccumulatedRevenueChart 
          points={vm.velocityQuery.data || EMPTY_VELOCITY}
          isLoading={vm.velocityQuery.isLoading} 
          isError={vm.velocityQuery.isError}
          isStale={vm.velocityQuery.isPlaceholderData}
          comparison="YearOverYear"
          className="h-full min-h-[350px] contained:min-h-0"
        />

        <TransactionDensityChart 
          response={vm.densityQuery.data || EMPTY_DENSITY}
          selectedPeriod={vm.densityPeriod}
          onPeriodChange={vm.setDensityPeriod}
          isLoading={vm.densityQuery.isLoading} 
          isError={vm.densityQuery.isError}
          isStale={vm.densityQuery.isPlaceholderData}
          className="h-full min-h-[350px] contained:min-h-0"
        />

        <CrossSegmentDistributionChart 
          response={vm.crossSegmentDistributionData || EMPTY_CROSS_SEGMENT_DISTRIBUTION}
          onTenantSelect={vm.handleTenantSelect} 
          isLoading={vm.crossSegmentDistributionQuery.isLoading} 
          isError={vm.crossSegmentDistributionQuery.isError}
          isStale={vm.crossSegmentDistributionQuery.isPlaceholderData}
          className="h-full min-h-[350px] contained:min-h-0"
        />

        <PortfolioImpactMatrixChart 
          portfolioImpact={vm.portfolioImpactData || EMPTY_PORTFOLIO_IMPACT}
          comparison="YearOverYear"
          onTenantSelect={vm.handleTenantSelect} 
          isLoading={vm.portfolioImpactQuery.isLoading} 
          isError={vm.portfolioImpactQuery.isError}
          isStale={vm.portfolioImpactQuery.isPlaceholderData}
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
            aria-label="Open tenant settings"
            title="Open tenant settings"
            onClick={() => void navigate({ to: '/settings/tenants' })}
            className="!min-h-14 !text-base m3-elevation-1"
          >
            Tenant settings
          </Button>
          <FinancialFilterMenu
            tenants={vm.tenantOptions}
            selectedTenantId={vm.tenantId ?? null}
            selectedTypes={vm.selectedTenantTypes}
            isLoading={vm.tenantsQuery.isLoading}
            onTenantChange={vm.handleTenantChange}
            onTypesChange={vm.setSelectedTenantTypes}
            onClearAll={vm.clearFinancialFilters}
          />
          <div className="mx-1 h-6 w-px shrink-0 bg-outline-variant" aria-hidden="true" />
          <PeriodSelector from="/financial" />
        </div>
      </DashboardFooter>
      <MobileFooterActions
        activeCount={countActiveFilterGroups(Boolean(vm.tenantId), vm.selectedTenantTypes.length > 0)}
        clearLabel="Clear all financial filters"
        onClearAll={vm.clearFinancialFilters}
        settingsAction={{
          label: 'Tenant settings',
          onClick: () => void navigate({ to: '/settings/tenants' }),
        }}
      >
        <FinancialFilterPanel
          embedded
          tenants={vm.tenantOptions}
          selectedTenantId={vm.tenantId ?? null}
          selectedTypes={vm.selectedTenantTypes}
          isLoading={vm.tenantsQuery.isLoading}
          onTenantChange={vm.handleTenantChange}
          onTypesChange={vm.setSelectedTenantTypes}
          onClearAll={vm.clearFinancialFilters}
        />
      </MobileFooterActions>
    </DashboardLayout>
  );
}
