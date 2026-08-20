// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import * as Generated from './generated';

export type ComparisonPeriod = Generated.ComparisonType;
export type GlobalKpi = Required<Generated.KpiResponseDto>;
export type FinancialKpi = GlobalKpi; // Deprecated but maps to KpiResponseDto
export type CumulativeGrowthDeltaPoint = Required<Generated.CumulativeGrowthDeltaPointResponseDto>;
export type NetGrowthAdditionPoint = Required<Generated.NetGrowthAdditionPointResponseDto>;
export type OrderBin = Required<Omit<Generated.OrderBinResponseDto, 'binLabel'>> & {
  binLabel: string;
};

export type PortfolioImpactTenant = Required<Omit<Generated.PortfolioImpactTenantResponseDto, 'tenantName'>> & {
  tenantName: string;
};

export type PortfolioImpactResponse = Required<Omit<Generated.PortfolioImpactResponseDto, 'tenants'>> & {
  tenants: PortfolioImpactTenant[];
};

export type RevenueEfficiencyTenant = Required<Omit<Generated.RevenueEfficiencyTenantResponseDto, 'tenantName'>> & {
  tenantName: string;
};

export type RevenueEfficiencyResponse = Required<Omit<Generated.RevenueEfficiencyResponseDto, 'tenants'>> & {
  tenants: RevenueEfficiencyTenant[];
};

export type CrossSegmentCohortTenant = Required<Omit<Generated.CrossSegmentCohortTenantResponseDto, 'tenantName'>> & {
  tenantName: string;
};

export type CrossSegmentCohortGroup = Required<Generated.CrossSegmentCohortGroupResponseDto>;

export type CrossSegmentDistributionResponse = Required<Omit<Generated.CrossSegmentDistributionResponseDto, 'cohorts' | 'tenants'>> & {
  cohorts: CrossSegmentCohortGroup[];
  tenants: CrossSegmentCohortTenant[];
};

export type UptimeMonitorDto = {
  id: number;
  tenantId: string;
  tenantName?: string;
  type: string;
  name: string;
  url: string;
  updateInterval: number;
  latencyDegradedFloor: number | null;
  uptimeSla: number | null;
  currentUptimePercentage: number | null;
  currentLatency: number | null;
  uptimeMonitorEnabled: boolean;
  currentStatus: string;
  lastUpdate: string | null;
  lastUptimeUpdate: string | null;
  lastLatencyUpdate: string | null;
  createdDate: string;
  lastSyncError: string | null;
  tags?: string[] | null;
  tenantBaseUrl: string | null;
  tenantImageUrl: string | null;
  httpMethod: string | null;
  timeoutSeconds: number | null;
  sslExpiresAt: string | null;
  domainExpiresAt: string | null;
  monitoredRegions: string[];
  currentStateDurationSeconds: number | null;
  latestIncident: Generated.MonitorIncidentDto | null;
};

export type TenantResponseDto = {
  id: string;
  name: string;
  type: Generated.TenantType;
  orderProvider: string;
  orderProviderSettings: Record<string, string | null> | null;
  orderProviderConfiguredSecretKeys: string[];
  imageUrl: string | null;
  currentlyFetching: boolean;
  fetchedFrom: string | null;
  fetchedUntil: string | null;
  lastPolled: string | null;
  orderFetchingEnabled: boolean;
  monitorCount: number;
  lastSyncError: string | null;
  hasOrderProviderSettings: boolean;
};

export type SystemHealthDto = {
  databaseStatus: string;
  hangfire: Required<Omit<Generated.HangfireHealthDto, 'status'>> & { status: string };
  sync: Required<Omit<Generated.SyncHealthDto, 'status' | 'globalSyncError'>> & { status: string; globalSyncError: string | null };
  lastLitiumSync: string | null;
  lastFleetUpdate: string | null;
  lastFleetUptimeUpdate: string | null;
  lastFleetLatencyUpdate: string | null;
};

export type BackgroundJobStatusDto = Required<Omit<Generated.BackgroundJobStatusDto, 'jobId' | 'jobName' | 'jobArgs' | 'state' | 'createdAt' | 'durationSeconds' | 'exceptionMessage'>> & {
  jobId: string;
  jobName: string;
  jobArgs: string | null;
  state: string;
  createdAt: string | null;
  durationSeconds: number | null;
  exceptionMessage: string | null;
};

export type UserResponseDto = {
  id: string;
  name: string;
  email: string | null;
  role: string;
};

export type GlobalConfigDto = Generated.GlobalConfigResponseDto;

// Added back for compatibility
export type AccumulatedRevenuePointDto = Required<Generated.AccumulatedRevenuePointResponseDto>;
export type LatencyPoint = Required<Generated.LatencyPointResponseDto>;
export type MonitorAnalyticsDto = Required<Omit<Generated.MonitorAnalyticsResponseDto, 'latencyPoints' | 'globalAverageLatency'>> & {
  globalAverageLatency: number | null;
  latencyPoints: LatencyPoint[];
};

// Keeping original because it's returned as an anonymous object from Hangfire endpoints
// and is not generated in the OpenAPI specs.
export interface RecurringJobDto {
  id: string;
  cron: string;
  nextExecution: string | null;
  lastExecution: string | null;
  lastJobState: string | null;
  lastJobDuration: number | null;
}

export * from './generated';
