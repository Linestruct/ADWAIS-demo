// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider, createRouter } from '@tanstack/react-router';
import { queryClient } from './queryClient';
import { routeTree } from './routeTree.gen';
import './index.css';

import { AuthProvider, hasAuthParams } from 'react-oidc-context';
import { isOidcConfigured, userManager } from './utils/oidcConfig';
import { applyFlexGapFallbackClass } from './utils/flexGapSupport';
import { Md3RippleProvider } from './components/common/ui/Md3RippleProvider';

applyFlexGapFallbackClass();

const router = createRouter({
  routeTree,
  context: {
    queryClient,
  },
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}

async function bootstrap() {
  // Complete the OIDC redirect callback before the router or AuthProvider mount,
  // so the router's beforeLoad guard never races an in-flight token exchange.
  if (isOidcConfigured && hasAuthParams()) {
    try {
      await userManager!.signinRedirectCallback();
      window.history.replaceState({}, document.title, window.location.pathname);
    } catch (error) {
      console.error('OIDC signin callback failed:', error);
      window.location.replace('/login');
      return;
    }
  }

  const app = (
    <QueryClientProvider client={queryClient}>
      <Md3RippleProvider>
        <RouterProvider router={router} />
      </Md3RippleProvider>
    </QueryClientProvider>
  );

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      {isOidcConfigured
        ? (
          <AuthProvider userManager={userManager!}>
            {app}
          </AuthProvider>
        )
        : app}
    </StrictMode>,
  );
}

bootstrap();
