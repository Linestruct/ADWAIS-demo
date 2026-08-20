// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {Menu, Settings, X} from 'lucide-react';
import type {Timeframe} from '../../../schemas';
import {KioskControls} from '../dashboard/KioskControls';
import {NotificationToggleWidget} from '../dashboard/NotificationToggleWidget';
import {NavLink} from './NavLink';
import {ConnectivityStatus} from './ConnectivityStatus';
import {UserAccountLink} from './UserAccountLink';
import {BrandLogoLink} from './BrandLogoLink';
import {useMediaQuery} from '../../../hooks/useMediaQuery';

type SiteHeaderProps = {
  financialTimeframe: Timeframe;
  fleetTimeframe: Timeframe;
  isMobileMenuOpen: boolean;
  isOnline: boolean;
  isBackendOnline: boolean;
  userLabel: string | null;
  onToggleMobileMenu: () => void;
  isProgressBarVisible: boolean;
};

export function SiteHeader({
  financialTimeframe,
  fleetTimeframe,
  isMobileMenuOpen,
  isOnline,
  isBackendOnline,
  userLabel,
  onToggleMobileMenu,
  isProgressBarVisible,
}: SiteHeaderProps) {
  const isMobileView = useMediaQuery('(max-width: 1023px)');

  return (
    <header className="relative z-10 shrink-0 bg-brand-bg-secondary px-6 py-3">
      {isMobileView ? (
        <div className="flex w-full items-center justify-between" data-header="mobile-bar">
          <BrandLogoLink
            timeframe={financialTimeframe}
            className="text-2xl leading-none text-white"
          />
          <div className="flex items-center gap-4">
            <ConnectivityStatus isOnline={isOnline} isBackendOnline={isBackendOnline} variant="mobile" />
            <UserAccountLink label={userLabel} variant="mobile" />
            <button
              onClick={onToggleMobileMenu}
              className="flex h-11 w-11 items-center justify-center rounded-full text-white transition-colors hover:bg-white/10 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-accent"
              aria-label="Toggle menu"
              aria-expanded={isMobileMenuOpen}
            >
              {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
            </button>
          </div>
        </div>
      ) : (
        <div className="flex w-full flex-wrap items-center justify-end gap-x-8 gap-y-4">
          <div className="flex flex-1 min-w-[100px] shrink-0" data-header="logo">
            <BrandLogoLink
              timeframe={financialTimeframe}
              className="text-4xl leading-none text-white"
            />
          </div>

          <nav className="flex flex-0 whitespace-nowrap items-center justify-center gap-4" data-header="nav">
            <NavLink to="/financial" search={{timeframe: financialTimeframe}}>
              Financial
            </NavLink>
            <NavLink to="/fleet-status" search={{timeframe: fleetTimeframe}}>
              Fleet status
            </NavLink>
            <NavLink to="/intranet">Intranet</NavLink>
            <NavLink to="/settings"><Settings size={20} /></NavLink>
          </nav>

          <div className="flex flex-1 shrink-0 items-center justify-end gap-2" data-header="controls">
            <div className="flex items-center gap-8">
              <ConnectivityStatus isOnline={isOnline} isBackendOnline={isBackendOnline} variant="desktop" />
              <UserAccountLink label={userLabel} variant="desktop" />
            </div>
            <NotificationToggleWidget />
            <KioskControls />
          </div>
          <style>{`
            @keyframes loading-bar {
              0% { left: -35%; width: 35%; }
              100% { left: 100%; width: 35%; }
            }
            .animate-loading-bar {
              animation: loading-bar 1.6s cubic-bezier(0.4, 0, 0.2, 1) infinite;
            }
          `}</style>
          <div 
            className="absolute bottom-0 left-0 right-0 h-[6px] translate-y-full z-50 overflow-hidden bg-primary-container pointer-events-none transition-opacity duration-500"
            style={{ opacity: isProgressBarVisible ? 1 : 0 }}
          >
            <div 
              className="absolute top-0 bottom-0 animate-loading-bar rounded-full" 
              style={{
                left: '-35%',
                width: '35%',
                background: 'linear-gradient(90deg, var(--color-brand-accent-transparent) 0%, var(--color-brand-accent-95) 50%, var(--color-brand-accent) 100%)',
                boxShadow: '0 0 16px 3px var(--color-brand-accent), 0 0 8px 1px var(--color-brand-accent), 0 0 4px var(--color-brand-accent)'
              }}
            />
          </div>
        </div>
      )}
    </header>
  );
}
