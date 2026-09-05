// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { ShieldAlert } from 'lucide-react';

export function AccessDeniedCard({ message = "You don't have permission to view this component." }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center p-8 bg-brand-bg-primary border border-outline rounded-xl text-center h-full min-h-[200px]">
      <div className="w-12 h-12 rounded-full bg-red-50 flex items-center justify-center mb-4 text-red-500">
        <ShieldAlert size={24} />
      </div>
      <h3 className="text-lg font-bold text-on-surface mb-2">Access Denied</h3>
      <p className="text-sm text-on-surface-variant max-w-sm">
        {message}
      </p>
    </div>
  );
}
