// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React from 'react';

interface FormSkeletonProps {
  children: React.ReactNode;
  className?: string;
}

export function FormSkeleton({ children, className = '' }: FormSkeletonProps) {
  return (
    <div className={`animate-pulse flex flex-col gap-10 py-2 ${className}`}>
      {children}
    </div>
  );
}

FormSkeleton.Input = function FormSkeletonInput({ labelWidth = 'w-28' }: { labelWidth?: string }) {
  return (
    <div className="flex flex-col gap-3">
      <div className={`h-3 bg-slate-250 rounded ${labelWidth}`} />
      <div className="h-9 bg-surface-container rounded-lg" />
    </div>
  );
};

FormSkeleton.Checkbox = function FormSkeletonCheckbox({ textWidth = 'w-36' }: { textWidth?: string }) {
  return (
    <div className="flex items-center gap-6 py-1">
      <div className="h-4 bg-slate-250 rounded w-4" />
      <div className={`h-4 bg-surface-container rounded ${textWidth}`} />
    </div>
  );
};
