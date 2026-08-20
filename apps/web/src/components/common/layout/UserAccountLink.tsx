// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {Link} from '@tanstack/react-router';
import { User } from 'lucide-react';

type UserAccountLinkProps = {
  label: string | null;
  variant: 'mobile' | 'desktop';
};

export function UserAccountLink({label, variant}: UserAccountLinkProps) {
  if (!label) return null;

  return (
    <Link data-md3-ripple
      to="/settings/authentication"
      className={variant === 'mobile'
        ? 'inline-flex min-h-10 max-w-[160px] items-center gap-1.5 truncate rounded-full bg-secondary-container px-3 text-sm font-bold hover:bg-surface-container-highest hover:m3-elevation-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary'
        : 'inline-flex min-h-11 max-w-[220px] items-center gap-2 truncate rounded-full bg-primary-container px-4 font-bold hover:bg-surface-container-highest hover:m3-elevation-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary whitespace-nowrap'}
    >
      <User size={variant === 'mobile' ? 16 : 16} className="shrink-0" />
    </Link>
  );
}
