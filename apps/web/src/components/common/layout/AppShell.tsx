// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState, useEffect } from 'react';
import { Outlet, useRouterState } from '@tanstack/react-router';
import { useIsFetching, useIsMutating } from '@tanstack/react-query';
import type { Timeframe } from '../../../schemas';
import type { PersistentDomain } from '../../../utils/timeframeStorage';
import { MobileFooterPill } from '../ui/mobileFooterPill';
import { SiteHeader } from './SiteHeader';
import { MobileNavigationMenu } from './MobileNavigationMenu';
import { useMediaQuery } from '../../../hooks/useMediaQuery';
import { useVisualViewportCssVars } from '../../../hooks/useVisualViewportCssVars';
import { useOrderNotifier } from '../../../hooks/useOrderNotifier';
import { MobileFooterActionsSlotContext } from '../ui/MobileFooterActionsContext';
import { RightSidebarSlotContext } from '../ui/RightSidebarSlotContext';

type AppShellProps = {
  pathname: string;
  isKioskRoute: boolean;
  isMobileMenuOpen: boolean;
  isOnline: boolean;
  isBackendOnline: boolean;
  userLabel: string | null;
  financialTimeframe: Timeframe;
  fleetTimeframe: Timeframe;
  timeframeDomain: PersistentDomain | null;
  onCloseMobileMenu: () => void;
  onToggleMobileMenu: () => void;
};

export function AppShell({
  pathname,
  isKioskRoute,
  isMobileMenuOpen,
  isOnline,
  isBackendOnline,
  userLabel,
  financialTimeframe,
  fleetTimeframe,
   timeframeDomain,
  onCloseMobileMenu,
  onToggleMobileMenu,
}: AppShellProps) {
  const isMobileView = useMediaQuery('(max-width: 767px)');
  useVisualViewportCssVars();
  useOrderNotifier();

  const isFetching = useIsFetching();
  const isMutating = useIsMutating();
  const isNavigating = useRouterState({ select: (s) => s.status === 'pending' });

  const showProgressBar = isNavigating || isFetching > 0 || isMutating > 0;

  const [isProgressBarVisible, setIsProgressBarVisible] = useState(false);
  const [mobileFooterActionsPanel, setMobileFooterActionsPanel] = useState<HTMLDivElement | null>(null);
  const [mobileFooterActionsIndicator, setMobileFooterActionsIndicator] = useState<HTMLSpanElement | null>(null);
  const [mobileFooterQuickAction, setMobileFooterQuickAction] = useState<HTMLDivElement | null>(null);
  const [rightSidebarContainer, setRightSidebarContainer] = useState<HTMLElement | null>(null);

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;
    
    if (showProgressBar) {
      const defer = setTimeout(() => {
        setIsProgressBarVisible(true);
      }, 0);
      return () => clearTimeout(defer);
    } else {
      timer = setTimeout(() => {
        setIsProgressBarVisible(false);
      }, 600);
      return () => clearTimeout(timer);
    }
  }, [showProgressBar]);

  return (
    <RightSidebarSlotContext.Provider value={{ container: rightSidebarContainer }}>
    <MobileFooterActionsSlotContext.Provider value={{
      panel: mobileFooterActionsPanel,
      indicator: mobileFooterActionsIndicator,
      quickAction: mobileFooterQuickAction,
    }}>
    <div className="app-shell flex flex-col bg-brand-bg-tertiary overflow-hidden font-sans text-brand-text">
      {!isKioskRoute && (
        <>
          <SiteHeader
            financialTimeframe={financialTimeframe}
            fleetTimeframe={fleetTimeframe}
            isMobileMenuOpen={isMobileMenuOpen}
            isOnline={isOnline}
            isBackendOnline={isBackendOnline}
            userLabel={userLabel}
            onToggleMobileMenu={onToggleMobileMenu}
            isProgressBarVisible={isProgressBarVisible}
          />

          <MobileNavigationMenu
            isOpen={isMobileMenuOpen}
            pathname={pathname}
            financialTimeframe={financialTimeframe}
            fleetTimeframe={fleetTimeframe}
            onClose={onCloseMobileMenu}
          />

          {isMobileView && timeframeDomain && (
            <div className="mobile-float-pills">
              <MobileFooterPill
                domain={timeframeDomain}
                hasPageActions={pathname.includes('/fleet-status') || pathname.includes('/financial')}
                pageActionsPanelRef={setMobileFooterActionsPanel}
                pageActionsIndicatorRef={setMobileFooterActionsIndicator}
                pageActionsQuickRef={setMobileFooterQuickAction}
              />
            </div>
          )}
        </>
      )}

      <main className="app-main flex-1 min-h-0 relative flex flex-row">
        <div className="app-main-scroll bg-surface-dim flex-1 min-w-0 w-full p-3 relative flex flex-col min-h-0 overflow-y-auto contained:overflow-hidden custom-scrollbar">
          <div className="flex flex-col flex-1 min-h-0">
            <Outlet />
          </div>
        </div>
        <aside
          ref={setRightSidebarContainer}
          className="hidden lg:flex w-60 shrink-0 flex-col bg-surface-container m3-elevation-3 z-10 empty:hidden"
        />
      </main>
    </div>
    </MobileFooterActionsSlotContext.Provider>
    </RightSidebarSlotContext.Provider>
  );
}
