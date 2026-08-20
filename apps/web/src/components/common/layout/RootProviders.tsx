// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useRef, type ReactNode } from 'react';
// import {TanStackRouterDevtools} from '@tanstack/react-router-devtools';
// import {ReactQueryDevtools} from '@tanstack/react-query-devtools';
import {Toaster} from 'sonner';
import { toast } from 'sonner';
import { useRouterState } from '@tanstack/react-router';
import {KioskProvider} from '../dashboard/KioskProvider';

export function RootProviders({children}: { children: ReactNode }) {
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
