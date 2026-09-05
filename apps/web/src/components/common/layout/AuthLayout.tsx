// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';
import { Settings } from 'lucide-react';
import { FactPanel } from '../dashboard/FactPanel';
import { AccumulatedRevenueChart } from '../../financial/AccumulatedRevenueChart';
import { TransactionDensityChart } from '../../financial/TransactionDensityChart';
import { RevenueEfficiencyChart } from '../../financial/RevenueEfficiencyChart';
import { PortfolioImpactMatrixChart } from '../../financial/PortfolioImpactMatrixChart';
import { MOCK_ACCUMULATED_REVENUE, MOCK_TRANSACTION_DENSITY_RESPONSE, MOCK_EFFICIENCY, MOCK_PORTFOLIO_IMPACT } from '../../../utils/mockKioskData';

interface AuthLayoutProps {
  children: ReactNode;
}

export function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <div className="fixed inset-0 flex items-center justify-center font-sans text-brand-text overflow-hidden bg-brand-bg-tertiary z-50">
      {/* Blurred Dashboard Background */}
      <div className="absolute inset-0 z-0 overflow-hidden pointer-events-none flex flex-col">
        <div className="auth-dashboard-mock flex flex-col min-h-full w-full landscape-lg:min-w-[1180px]">
          {/* Mock Header */}
          <header
            className="relative flex flex-col md:grid md:grid-cols-[auto_1fr_auto] items-center px-4 md:px-6 py-3 shrink-0 bg-brand-bg-secondary z-10 gap-3 md:gap-6 min-w-0"
            style={{ borderBottomColor: 'var(--color-brand-bg-secondary-20)' }}
          >
            <div className="flex justify-center md:justify-start min-w-0">
              <span className="font-display text-white whitespace-nowrap font-black uppercase tracking-tight text-xl">ADWAIS</span>
            </div>
            <nav className="flex justify-center items-center gap-6 md:gap-16 font-black uppercase tracking-wide text-xs md:text-sm min-w-0 max-w-full overflow-hidden">
              <span className="text-brand-accent border-b-2 border-brand-accent pb-1">Financial</span>
              <span className="text-white/55 whitespace-nowrap">Fleet status</span>
              <span className="text-white/55">Intranet</span>
              <span className="text-white/55"><Settings size={20} /></span>
            </nav>
            <div className="flex justify-center md:justify-end items-center gap-8 min-w-0">
              <div className="flex items-center gap-4 md:gap-6 px-2.5 md:px-3 py-1.5 rounded-lg border bg-amber-100 text-on-surface border-amber-400 shadow-sm cursor-not-allowed opacity-90 max-w-full">
                <span className="relative flex h-2.5 w-2.5">
                  <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-amber-500"></span>
                </span>
                <span className="text-xs md:text-sm font-black uppercase tracking-wide truncate">Interactive</span>
              </div>
            </div>
          </header>

          <div className="w-full flex-1 min-h-0 p-4 md:p-6 overflow-hidden">
            <div className="flex flex-col gap-8 min-h-full">
            <section className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-5 gap-4 shrink-0">
              <FactPanel label="Global Revenue (T30)" value="201 634 127 kr" extra={{ type: 'PoP', value: -2.74 }} />
              <FactPanel label="Transaction Volume" value="236 800" extra={{ type: 'PoP', value: -2.58 }} />
              <FactPanel label="Portfolio AOV" value="851.5 SEK" extra={{ type: 'PoP', value: -0.17 }} />
              <FactPanel label="Active Tenants" value="40" extra={{ type: 'PoP', value: 0.00 }} />
              <FactPanel label="Avg Revenue Per Tenant" value="5 mn" extra={{ type: 'PoP', value: -2.74 }} />
            </section>

            <div className="flex flex-grow h-full">
              <section className="grid grid-cols-1 landscape-lg:grid-cols-2 gap-4 flex-1 min-h-0 auto-rows-fr">
              <AccumulatedRevenueChart points={MOCK_ACCUMULATED_REVENUE} className="h-full min-h-0" />
              <TransactionDensityChart response={MOCK_TRANSACTION_DENSITY_RESPONSE} className="h-full min-h-0" />
              <RevenueEfficiencyChart response={MOCK_EFFICIENCY} onTenantSelect={() => { }} className="h-full min-h-0" />
              <PortfolioImpactMatrixChart portfolioImpact={MOCK_PORTFOLIO_IMPACT} onTenantSelect={() => { }} className="h-full min-h-0" />
              </section>
            </div>
            </div>
          </div>
        </div>

        {/* Moderate Blur Overlay to preserve vibrant colors */}
        <div
          className="absolute inset-0 backdrop-blur-sm z-10"
          style={{ backgroundColor: 'var(--md-sys-color-surface-45)' }}
        />
      </div>

      {/* Auth Content */}
      <div className="relative z-30 w-full flex justify-center px-4">
        {children}
      </div>
    </div>
  );
}
