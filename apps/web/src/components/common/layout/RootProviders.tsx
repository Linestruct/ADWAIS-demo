// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useRef, type ReactNode } from 'react';
// import {TanStackRouterDevtools} from '@tanstack/react-router-devtools';
// import {ReactQueryDevtools} from '@tanstack/react-query-devtools';
import {Toaster} from 'sonner';
import { toast } from 'sonner';
import { AlertCircle, CheckCircle2, Info } from 'lucide-react';
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
      <Toaster
        position="top-center"
        offset={{ top: '4.5rem' }}
        mobileOffset={{ top: '4.5rem' }}
        closeButton
        theme="light"
        toastOptions={{
          unstyled: true,
          classNames: {
            toast: 'flex w-[356px] max-w-[calc(100vw-2rem)] items-start gap-3 rounded-xl p-4 pr-9 text-sm m3-elevation-2',
            title: 'font-bold',
            description: 'mt-0.5 font-medium leading-relaxed break-words',
            icon: 'mt-0.5 shrink-0',
            error: 'bg-error-container text-on-error-container',
            success: 'bg-success-container text-on-success-container',
            info: 'bg-secondary-container text-on-secondary-container',
            closeButton: 'absolute right-2 top-2 rounded-md p-1 opacity-60 transition-opacity hover:opacity-100',
          },
        }}
        icons={{
          error: <AlertCircle size={16} className="text-error" aria-hidden="true" />,
          success: <CheckCircle2 size={16} className="text-success" aria-hidden="true" />,
          info: <Info size={16} className="text-secondary" aria-hidden="true" />,
        }}
      />
    </KioskProvider>
  );
}
