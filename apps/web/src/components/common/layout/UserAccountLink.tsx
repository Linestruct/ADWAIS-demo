// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import {Link} from '@tanstack/react-router';
import { User } from 'lucide-react';

type UserAccountLinkProps = {
  label: string | null;
};

export function UserAccountLink({label}: UserAccountLinkProps) {
  if (!label) return null;

  return (
    <Link data-md3-ripple
      to="/settings/authentication"
      className="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-primary-container font-bold hover:bg-surface-container-highest hover:m3-elevation-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
    >
      <User size={16} className="shrink-0" />
    </Link>
  );
}
