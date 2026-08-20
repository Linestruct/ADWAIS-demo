// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {createRootRoute, redirect, useRouterState, useSearch} from '@tanstack/react-router';
import {useContext} from 'react';
import {AuthContext} from 'react-oidc-context';
import {userManager} from '../utils/oidcConfig';
import {getKioskToken} from '../utils/auth';
import {getSavedTimeframe, type PersistentDomain} from '../utils/timeframeStorage';
import {useCurrentUser} from '../hooks/useCurrentUser';
import {useConnectivityStatus} from '../hooks/useConnectivityStatus';
import {useMobileMenu} from '../hooks/useMobileMenu';
import {RootProviders} from '../components/common/layout/RootProviders';
import {AuthRouteShell} from '../components/common/layout/AuthRouteShell';
import {AppShell} from '../components/common/layout/AppShell';

export const Route = createRootRoute({
  beforeLoad: async ({location}) => {
    const isLoginRoute = location.pathname === '/login';
    const isKioskRoute = location.pathname.startsWith('/kiosk');

    if (isLoginRoute || isKioskRoute) return;

    const user = await userManager?.getUser();
    if (user && !user.expired) return;
    if (getKioskToken()) return;

    throw redirect({
      to: '/login',
    });
  },
  component: RootComponent,
});

function RootComponent() {
  useSearch({strict: false});

  const auth = useContext(AuthContext);
  const {user} = useCurrentUser();
  const {isOnline, isBackendOnline} = useConnectivityStatus();
  const location = useRouterState({select: (state) => state.location});
  const mobileMenu = useMobileMenu(location.pathname);

  const financialTimeframe = getSavedTimeframe('/financial');
  const fleetTimeframe = getSavedTimeframe('/fleet-status');
  const isFinancialPage = location.pathname === '/financial' || location.pathname.startsWith('/financial/');
  const isFleetPage = location.pathname === '/fleet-status' || location.pathname.startsWith('/fleet-status/');
  const timeframeDomain: PersistentDomain | null = isFinancialPage ? '/financial' : isFleetPage ? '/fleet-status' : null;

  const kioskToken = getKioskToken();
  const userLabel = user?.name || auth?.user?.profile?.name || (kioskToken ? 'Kiosk' : null);
  const isAuthRoute = location.pathname === '/login' || location.pathname.startsWith('/kiosk');

  return (
    <RootProviders>
      {isAuthRoute ? (
        <AuthRouteShell routeKey={location.pathname} />
      ) : (
        <AppShell
          pathname={location.pathname}
          isKioskRoute={location.pathname.startsWith('/kiosk')}
          isMobileMenuOpen={mobileMenu.isOpen}
          isOnline={isOnline}
          isBackendOnline={isBackendOnline}
          userLabel={userLabel}
          financialTimeframe={financialTimeframe}
          fleetTimeframe={fleetTimeframe}
          timeframeDomain={timeframeDomain}
          onCloseMobileMenu={mobileMenu.close}
          onToggleMobileMenu={mobileMenu.toggle}
        />
      )}
    </RootProviders>
  );
}
