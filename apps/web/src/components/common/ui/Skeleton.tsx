// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React from 'react';

interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  className?: string;
}

export function Skeleton({ className = '', ...props }: SkeletonProps) {
  return (
    <div
      className={`animate-pulse bg-surface-container rounded-md ${className}`}
      {...props}
    />
  );
}

Skeleton.Card = function SkeletonCard({ className = '', ...props }: SkeletonProps) {
  return (
    <Skeleton
      className={`bg-surface-container-high/70 rounded-xl ${className}`}
      {...props}
    />
  );
};
