// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React from 'react';
import { Lock, Loader2 } from 'lucide-react';

interface SecureButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  locked?: boolean;
  lockTitle?: string;
  loading?: boolean;
  loadingText?: string;
  icon?: React.ReactNode;
}

export function SecureButton({
  locked = false,
  lockTitle = 'Requires higher privileges',
  loading = false,
  loadingText,
  icon,
  children,
  className = '',
  disabled,
  ...props
}: SecureButtonProps) {
  const isButtonDisabled = disabled || loading || locked;

  const disabledStateClasses =
    'disabled:cursor-not-allowed disabled:bg-on-surface/[0.12] disabled:text-on-surface/[0.38] disabled:shadow-none disabled:hover:bg-on-surface/[0.12] disabled:hover:text-on-surface/[0.38]';

  // Strip conflicting color, state, and pointer classes if locked to ensure a clean disabled style.
  const combinedClassName = locked
    ? className
        .replace(/(bg|text|border|hover|active|shadow|cursor|opacity)-\S+/g, '')
        .replace(/(hover|active|focus|disabled):\S+/g, '')
        .trim() + ' bg-on-surface/[0.12] text-on-surface/[0.38] font-bold cursor-not-allowed shadow-none flex items-center justify-center gap-2'
    : `${className} ${disabledStateClasses}`;

  return (
    <button
      {...props}
      disabled={isButtonDisabled}
      title={locked ? lockTitle : props.title}
      className={combinedClassName}
    >
      {loading ? (
        <>
          <Loader2 className="animate-spin shrink-0" size={16} />
          <span>{loadingText || children}</span>
        </>
      ) : (
        <>
          {locked ? <Lock className="shrink-0" size={16} /> : icon}
          <span>{children}</span>
        </>
      )}
    </button>
  );
}
