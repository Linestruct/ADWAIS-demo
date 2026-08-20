// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React from 'react';

interface AuthCardProps {
  children: React.ReactNode;
  className?: string;
}

export function AuthCard({ children, className = '' }: AuthCardProps) {
  return (
    <div
      className={`backdrop-blur-2xl p-8 sm:p-12 md:p-20 rounded-3xl sm:rounded-[2rem] shadow-[0_0_60px_rgba(0,0,0,0.08)] text-center w-full max-w-2xl min-h-[500px] sm:min-h-[590px] flex flex-col justify-between border border-white/20 relative z-20 overflow-hidden mx-4 sm:mx-0 ${className}`}
      style={{ backgroundColor: 'var(--color-brand-bg-primary-01)' }}
    >
      {children}
    </div>
  );
}
