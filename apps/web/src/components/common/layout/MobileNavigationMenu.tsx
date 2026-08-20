// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useState } from 'react';
import { Link } from '@tanstack/react-router';
import { ChevronDown, Settings } from 'lucide-react';
import type { Timeframe } from '../../../schemas';
import { NotificationToggleWidget } from '../dashboard/NotificationToggleWidget';

type MobileNavigationMenuProps = {
  isOpen: boolean;
  pathname: string;
  financialTimeframe: Timeframe;
  fleetTimeframe: Timeframe;
  onClose: () => void;
};

const mobileLinkClass = (isActive: boolean, extra = '') =>
  `flex min-h-12 w-full items-center rounded-full px-5 text-left text-sm font-bold transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary ${extra} ${
    isActive
      ? 'bg-surface-container-highest text-on-primary-container'
      : 'text-on-surface-variant hover:bg-surface-container-high hover:text-on-surface'
  }`;

export function MobileNavigationMenu({ isOpen, pathname, financialTimeframe, fleetTimeframe, onClose }: MobileNavigationMenuProps) {
  const isSettingsRoute = pathname.startsWith('/settings');
  const [wasOnSettingsRoute, setWasOnSettingsRoute] = useState(isSettingsRoute);
  const [isSettingsExpanded, setIsSettingsExpanded] = useState(isSettingsRoute);

  if (isSettingsRoute !== wasOnSettingsRoute) {
    setWasOnSettingsRoute(isSettingsRoute);
    setIsSettingsExpanded(isSettingsRoute);
  }

  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  return (
    <div
      className={`fixed inset-x-0 bottom-0 top-[60px] z-40 xl:hidden ${isOpen ? 'pointer-events-auto' : 'pointer-events-none'}`}
      id="mobile-menu"
      role="dialog"
      aria-modal={isOpen ? true : undefined}
      aria-hidden={!isOpen}
      aria-label="Main navigation"
    >
      <button
          data-md3-ripple="off"
        type="button"
        className={`absolute inset-0 cursor-default bg-black/45 backdrop-blur-[1px] transition-opacity duration-300 ${isOpen ? 'opacity-100' : 'opacity-0'}`}
        onClick={onClose}
        aria-label="Close navigation menu"
        tabIndex={isOpen ? 0 : -1}
      />

      <aside
        inert={!isOpen}
        className={`m3-elevation-3 relative ml-auto flex h-full w-[320px] max-w-[calc(100vw-24px)] flex-col overflow-hidden bg-surface-container text-on-surface transition-transform ${isOpen ? 'translate-x-0 duration-[400ms] ease-[cubic-bezier(0.05,0.7,0.1,1)]' : 'translate-x-full duration-200 ease-[cubic-bezier(0.3,0,0.8,0.15)]'}`}
      >
        <div className="custom-scrollbar flex min-h-0 flex-1 flex-col overflow-y-auto p-2">
          <nav className="flex flex-col gap-1" aria-label="Main navigation links">
            <Link data-md3-ripple to="/financial" search={{ timeframe: financialTimeframe }} onClick={onClose} className={mobileLinkClass(pathname === '/financial')} aria-current={pathname === '/financial' ? 'page' : undefined}>Financial</Link>
            <Link data-md3-ripple to="/fleet-status" search={{ timeframe: fleetTimeframe }} onClick={onClose} className={mobileLinkClass(pathname.startsWith('/fleet-status'))} aria-current={pathname.startsWith('/fleet-status') ? 'page' : undefined}>Fleet Status</Link>
            <Link data-md3-ripple to="/intranet" onClick={onClose} className={mobileLinkClass(pathname === '/intranet')} aria-current={pathname === '/intranet' ? 'page' : undefined}>Intranet</Link>
            <button
              type="button"
              onClick={() => setIsSettingsExpanded(current => !current)}
              className={mobileLinkClass(pathname.startsWith('/settings'), 'justify-between gap-3')}
              aria-expanded={isSettingsExpanded}
              aria-controls="mobile-settings-navigation"
            >
              <span className="flex items-center gap-4"><Settings size={19} aria-hidden="true" />Settings</span>
              <ChevronDown size={20} aria-hidden="true" className={`transition-transform ${isSettingsExpanded ? 'rotate-180' : ''}`} />
            </button>
            {isSettingsExpanded && (
              <div id="mobile-settings-navigation" role="group" aria-label="Settings navigation links" className="ml-4 flex flex-col gap-1 border-l border-outline-variant pl-2">
                <Link data-md3-ripple to="/settings/jobs" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/jobs'), 'gap-3')} aria-current={pathname.startsWith('/settings/jobs') ? 'page' : undefined}>Background Jobs</Link>
                <Link data-md3-ripple to="/settings/configuration" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/configuration'))}>Configuration</Link>
                <Link data-md3-ripple to="/settings/tenants" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/tenants'))}>Tenants</Link>
                <Link data-md3-ripple to="/settings/monitors" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/monitors'))}>Monitors</Link>
                <Link data-md3-ripple to="/settings/events" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/events'))}>Events &amp; Health</Link>
                <Link data-md3-ripple to="/settings/users" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/users'))}>Users</Link>
                <Link data-md3-ripple to="/settings/authentication" onClick={onClose} className={mobileLinkClass(pathname.startsWith('/settings/authentication'))}>Authentication</Link>
              </div>
            )}
          </nav>

          <div className="mt-auto border-t border-outline-variant px-2 pt-4">
            <span className="mb-3 block px-2 text-xs font-black uppercase tracking-wide text-on-surface-variant">Controls</span>
            <NotificationToggleWidget />
          </div>
        </div>
      </aside>
    </div>
  );
}
