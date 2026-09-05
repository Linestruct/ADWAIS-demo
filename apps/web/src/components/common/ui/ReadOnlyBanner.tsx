// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Lock } from 'lucide-react';

interface ReadOnlyBannerProps {
  message: string;
}

export function ReadOnlyBanner({ message }: ReadOnlyBannerProps) {
  return (
    <div role="status" className="flex items-center gap-3 rounded-xl bg-surface-container-high px-4 py-3 text-sm font-semibold text-on-surface-variant animate-in fade-in duration-300">
      <Lock size={16} className="shrink-0" aria-hidden="true" />
      <span>{message}</span>
    </div>
  );
}
