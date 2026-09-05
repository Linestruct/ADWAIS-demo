// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { keepPreviousData } from '@tanstack/react-query';
import { useOrgSelection } from './useOrgSelection';
import { 
  useGetApiFinancialKpis,
  useGetApiFinancialAccumulatedRevenue,
  useGetApiFinancialPortfolioImpact,
  useGetApiFinancialRevenueEfficiency,
  useGetApiFinancialCrossSegmentDistribution,
  useGetApiFinancialDailyRevenueDelta,
  useGetApiFinancialCumulativeGrowthDelta,
  useGetApiFinancialOrderDistribution,
  useGetApiFinancialTransactionDensity
} from '../api/generated/endpoints';
import type { 
  ComparisonPeriod,
  GlobalKpi,
  PortfolioImpactResponse,
  RevenueEfficiencyResponse,
  CrossSegmentDistributionResponse,
  NetGrowthAdditionPoint,
  CumulativeGrowthDeltaPoint,
  OrderBin,
  AccumulatedRevenuePointDto,
  TransactionDensityResponseDto,
  TransactionDensityPeriod,
  Timeframe,
  ComparisonType,
  TenantType
} from '@types';

export const financialKeys = {
  all: (orgId: string | null) => ['financial', orgId] as const,
  kpis: (orgId: string | null, timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'kpis', timeframe, tenantId, comparison, tenantTypes] as const,
  velocity: (orgId: string | null, timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'velocity', timeframe, tenantId, comparison, tenantTypes] as const,
  extremes: (orgId: string | null, timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'extremes', timeframe, comparison, tenantTypes] as const,
  portfolioImpact: (orgId: string | null, timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'portfolioImpact', timeframe, comparison, tenantTypes] as const,
  revenueEfficiency: (orgId: string | null, timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'revenueEfficiency', timeframe, comparison, tenantTypes] as const,
  crossSegmentDistribution: (orgId: string | null, timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'crossSegmentDistribution', timeframe, comparison, tenantTypes] as const,
  volumeAnomaly: (orgId: string | null, timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'volumeAnomaly', timeframe, comparison, tenantTypes] as const,
  delta: (orgId: string | null, timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'delta', timeframe, tenantId, comparison, tenantTypes] as const,
  netGrowthAddition: (orgId: string | null, timeframe: string, tenantId?: string | null, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'netGrowthAddition', timeframe, tenantId, tenantTypes] as const,
  orders: (orgId: string | null, timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod) => [...financialKeys.all(orgId), 'orders', timeframe, tenantId, comparison] as const,
  accumulatedRevenue: (orgId: string | null, timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'accumulatedRevenue', timeframe, tenantId, comparison, tenantTypes] as const,
  transactionDensity: (orgId: string | null, period: TransactionDensityPeriod, tenantId?: string | null, tenantTypes?: TenantType[]) => [...financialKeys.all(orgId), 'transactionDensity', period, tenantId, tenantTypes] as const,
};

const REFETCH_INTERVAL = 60000;

export function useGlobalKpis(timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialKpis<GlobalKpi, Error>(
    { timeframe: timeframe as Timeframe, tenantId: tenantId || undefined, comparison: comparison as ComparisonType, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.kpis(selectedOrgId, timeframe, tenantId, comparison, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as GlobalKpi
      }
    }
  );
}

export function useAccumulatedRevenue(timeframe: string, tenantId?: string | null, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialAccumulatedRevenue<AccumulatedRevenuePointDto[], Error>(
    { timeframe: timeframe as Timeframe, tenantId: tenantId || undefined, comparison: comparison as ComparisonType, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.accumulatedRevenue(selectedOrgId, timeframe, tenantId, comparison, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as AccumulatedRevenuePointDto[]
      }
    }
  );
}

export function usePortfolioImpact(timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialPortfolioImpact<PortfolioImpactResponse, Error>(
    { timeframe: timeframe as Timeframe, comparison: comparison as ComparisonType, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.portfolioImpact(selectedOrgId, timeframe, comparison, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as PortfolioImpactResponse
      }
    }
  );
}

export function useRevenueEfficiency(timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialRevenueEfficiency<RevenueEfficiencyResponse, Error>(
    { timeframe: timeframe as Timeframe, comparison: comparison as ComparisonType, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.revenueEfficiency(selectedOrgId, timeframe, comparison, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as RevenueEfficiencyResponse
      }
    }
  );
}

export function useCrossSegmentDistribution(timeframe: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialCrossSegmentDistribution<CrossSegmentDistributionResponse, Error>(
    { timeframe: timeframe as Timeframe, comparison: comparison as ComparisonType, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.crossSegmentDistribution(selectedOrgId, timeframe, comparison, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as CrossSegmentDistributionResponse
      }
    }
  );
}

export function useCumulativeGrowthDelta(timeframe: string, tenantId: string, comparison?: ComparisonPeriod, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialCumulativeGrowthDelta<CumulativeGrowthDeltaPoint[], Error>(
    { timeframe: timeframe as Timeframe, tenantId, comparison: comparison as ComparisonType, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.delta(selectedOrgId, timeframe, tenantId, comparison, tenantTypes),
        enabled: !!tenantId,
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as CumulativeGrowthDeltaPoint[]
      }
    }
  );
}

export function useNetGrowthAddition(timeframe: string, tenantId?: string | null, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialDailyRevenueDelta<NetGrowthAdditionPoint[], Error>(
    { timeframe: timeframe as Timeframe, tenantId: tenantId || undefined, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.netGrowthAddition(selectedOrgId, timeframe, tenantId, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as NetGrowthAdditionPoint[]
      }
    }
  );
}

export function useOrderDistribution(timeframe: string, tenantId: string, comparison?: ComparisonPeriod) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialOrderDistribution<OrderBin[], Error>(
    { timeframe: timeframe as Timeframe, tenantId, comparison: comparison as ComparisonType },
    {
      query: {
        queryKey: financialKeys.orders(selectedOrgId, timeframe, tenantId, comparison),
        enabled: !!tenantId,
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as OrderBin[]
      }
    }
  );
}

export function useTransactionDensity(period: TransactionDensityPeriod, tenantId?: string | null, tenantTypes?: TenantType[]) {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiFinancialTransactionDensity<TransactionDensityResponseDto, Error>(
    { period, tenantId: tenantId || undefined, tenantTypes: tenantTypes?.length ? tenantTypes : undefined },
    {
      query: {
        queryKey: financialKeys.transactionDensity(selectedOrgId, period, tenantId, tenantTypes),
        refetchInterval: REFETCH_INTERVAL,
        placeholderData: keepPreviousData,
        select: (res) => res.data as TransactionDensityResponseDto
      }
    }
  );
}
