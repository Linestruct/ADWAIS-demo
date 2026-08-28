// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useEffect, useRef, type ReactNode } from 'react';
// import {TanStackRouterDevtools} from '@tanstack/react-router-devtools';
// import {ReactQueryDevtools} from '@tanstack/react-query-devtools';
import {Toaster} from 'sonner';
import { toast } from 'sonner';
import { useRouterState } from '@tanstack/react-router';
import {KioskProvider} from '../dashboard/KioskProvider';
import {useOrgSelectionWatcher} from '../../../hooks/useOrgSelectionWatcher';

export function RootProviders({children}: { children: ReactNode }) {
  useOrgSelectionWatcher();

  const pathname = useRouterState({select: (state) => state.location.pathname});
  const previousPathname = useRef(pathname);

  useEffect(() => {
    if (previousPathname.current !== pathname) {
      toast.dismiss();
      previousPathname.current = pathname;
    }
  }, [pathname]);

  return (
    <KioskProvider>
      {children}
      {/*{import.meta.env.DEV && <TanStackRouterDevtools position="bottom-right" />}*/}
      {/*{import.meta.env.DEV && <ReactQueryDevtools buttonPosition="bottom-left" />}*/}
      <Toaster closeButton richColors theme="light" />
    </KioskProvider>
  );
}
