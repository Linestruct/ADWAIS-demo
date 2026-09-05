// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createFileRoute, redirect } from '@tanstack/react-router';
import { getKioskToken } from '../utils/auth';
import { userManager } from '../utils/oidcConfig';

export const Route = createFileRoute('/login')({
  beforeLoad: async () => {
    const user = await userManager?.getUser();
    if (user && !user.expired) {
      throw redirect({ to: '/financial' });
    }
    if (getKioskToken()) {
      throw redirect({ to: '/fleet-status' });
    }
  },
});
