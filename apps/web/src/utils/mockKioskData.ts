// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { 
  AccumulatedRevenuePointDto, 
  CrossSegmentDistributionResponse,
  PortfolioImpactResponse, 
  RevenueEfficiencyResponse, 
  TransactionDensityPointResponseDto,
  TransactionDensityResponseDto,
} from '@types';

export const MOCK_ACCUMULATED_REVENUE: AccumulatedRevenuePointDto[] = Array.from({ length: 30 }).map((_, i) => ({
  timestamp: new Date(Date.now() - (29 - i) * 24 * 60 * 60 * 1000).toISOString(),
  currentRevenue: 5000 + i * 200,
  currentRevenueB2C: 2000 + i * 80,
  currentRevenueB2B: 2000 + i * 80,
  currentRevenueMixed: 1000 + i * 40,
  previousRevenue: 4800 + i * 190,
  currentAccumulated: (5000 + i * 200) * (i + 1),
  previousAccumulated: (4800 + i * 190) * (i + 1),
}));

export const MOCK_PORTFOLIO_IMPACT: PortfolioImpactResponse = {
  globalGrowthPercentage: 15.2,
  medianBaselineRevenue: 4500000,
  medianPortfolioShare: 2.2,
  tenants: Array.from({ length: 45 }).map((_, i) => ({
    tenantId: `t-${i}`,
    tenantName: `Mock Store ${i + 1}`,
    baselineRevenue: 1000000 + Math.random() * 8000000,
    growthPercentage: -25 + Math.random() * 65,
    currentRevenue: 1100000 + Math.random() * 9000000,
    portfolioSharePercentage: 0.2 + Math.random() * 6.5,
    orderVolume: 5000 + Math.random() * 20000,
    volumeGrowthPercentage: -10 + Math.random() * 30,
    previousRevenue: 1000000 + Math.random() * 8000000,
    type: i % 3 === 0 ? 'B2B' : i % 3 === 1 ? 'B2C' : 'Mixed',
    vertical: 'Retail',
    status: 'Active',
    orderProviderEndpoint: null
  }))
};

export const MOCK_EFFICIENCY: RevenueEfficiencyResponse = {
  globalAverageOrderValue: 850,
  medianOrderVolume: 12000,
  medianPortfolioShare: 0.05,
  tenants: Array.from({ length: 45 }).map((_, i) => ({
    tenantId: `t-${i}`,
    tenantName: `Mock Store ${i + 1}`,
    averageOrderValue: 200 + Math.random() * 1600,
    orderVolume: 5000 + Math.random() * 20000,
    portfolioShare: 0.01 + Math.random() * 0.15,
    portfolioSharePercentage: 1 + Math.random() * 15,
    growthVelocity: -5 + Math.random() * 25,
    currentRevenue: 1100000 + Math.random() * 9000000,
    transactionVolume: 5000 + Math.random() * 20000,
    type: i % 3 === 0 ? 'B2B' : i % 3 === 1 ? 'B2C' : 'Mixed',
    vertical: 'Retail',
    status: 'Active',
    orderProviderEndpoint: null
  }))
};

export const MOCK_CROSS_SEGMENT_DISTRIBUTION: CrossSegmentDistributionResponse = {
  cohorts: [
    { type: 'B2B', tenantCount: 15, medianAov: 14500, q1Aov: 8200, q3Aov: 28000, medianVolume: 1800, q1Volume: 850, q3Volume: 4200, medianRevenue: 26100000, q1Revenue: 6970000, q3Revenue: 117600000 },
    { type: 'B2C', tenantCount: 15, medianAov: 680, q1Aov: 420, q3Aov: 1150, medianVolume: 24000, q1Volume: 12000, q3Volume: 48000, medianRevenue: 16320000, q1Revenue: 5040000, q3Revenue: 55200000 },
    { type: 'Mixed', tenantCount: 15, medianAov: 2800, q1Aov: 1400, q3Aov: 5600, medianVolume: 8500, q1Volume: 3500, q3Volume: 16000, medianRevenue: 23800000, q1Revenue: 4900000, q3Revenue: 89600000 }
  ],
  tenants: Array.from({ length: 45 }).map((_, i) => {
    const type = i % 3 === 0 ? 'B2B' : i % 3 === 1 ? 'B2C' : 'Mixed';
    const aov = type === 'B2B' ? 4000 + Math.random() * 45000 : type === 'B2C' ? 250 + Math.random() * 1500 : 900 + Math.random() * 8000;
    const vol = type === 'B2B' ? 500 + Math.floor(Math.random() * 5000) : type === 'B2C' ? 8000 + Math.floor(Math.random() * 60000) : 2000 + Math.floor(Math.random() * 25000);
    const rev = Math.round(aov * vol);
    return {
      tenantId: `t-${i}`,
      tenantName: `Mock Store ${i + 1}`,
      type,
      averageOrderValue: Math.round(aov),
      orderVolume: vol,
      periodRevenue: rev,
      portfolioSharePercentage: 0.5 + Math.random() * 8,
      aovPercentileRank: 50,
      volumePercentileRank: 50,
      revenuePercentileRank: 50,
      orderProviderEndpoint: null
    };
  })
};

export const MOCK_TRANSACTION_DENSITY: TransactionDensityPointResponseDto[] = Array.from({ length: 7 * 24 }).map((_, i) => {
  const dayOfWeek = Math.floor(i / 24) + 1;
  const hour = i % 24;
  const timeFactor = Math.sin((hour - 6) / 24 * 2 * Math.PI) + 1.2;
  const dayFactor = dayOfWeek >= 5 ? 1.3 : 0.9;
  const count = Math.floor((10 + Math.random() * 50) * timeFactor * dayFactor);
  const totalRevenue = count * (500 + Math.random() * 400);
  return {
    dayOfWeek,
    hour,
    count,
    totalRevenue,
  };
});

export const MOCK_TRANSACTION_DENSITY_RESPONSE: TransactionDensityResponseDto = {
  points: MOCK_TRANSACTION_DENSITY,
  totalCount: MOCK_TRANSACTION_DENSITY.reduce((sum, point) => sum + point.count, 0),
  minCount: Math.min(...MOCK_TRANSACTION_DENSITY.map(point => point.count)),
  maxCount: Math.max(...MOCK_TRANSACTION_DENSITY.map(point => point.count)),
  averageCountPerBucket: MOCK_TRANSACTION_DENSITY.reduce((sum, point) => sum + point.count, 0) / (7 * 24),
  sampleQuality: 'Stable',
  requestedPeriod: 'Auto',
  effectivePeriod: 'T30',
  timeZoneId: 'Europe/Stockholm',
  periodStart: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
  periodEnd: new Date().toISOString(),
};
