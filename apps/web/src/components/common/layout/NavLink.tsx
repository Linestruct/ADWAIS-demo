// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Link, type LinkOptions } from '@tanstack/react-router';
import type { ReactNode } from 'react';

interface NavLinkProps extends Omit<LinkOptions, 'className' | 'activeProps'> {
  children: ReactNode;
}

export function NavLink(props: NavLinkProps) {
  return (
    <Link
      {...props}
      activeOptions={{ includeSearch: false }}
      className="inline-flex min-h-11 items-center px-2 text-base font-extrabold text-white/60 transition-colors hover:text-white no-underline border-b-4 border-transparent uppercase tracking-wider"
      activeProps={{ className: '!text-brand-accent !border-brand-accent' }}
    >
      {props.children}
    </Link>
  );
}
