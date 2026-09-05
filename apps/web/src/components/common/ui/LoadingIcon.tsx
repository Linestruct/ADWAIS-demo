// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

interface LoadingIconProps {
  label?: string;
}

export function LoadingIcon({ label = 'Loading' }: LoadingIconProps) {
  return (
    <div className="loading-icon" role="status" aria-live="polite" aria-label={label}>
      <span className="loading-icon__spinner" />
    </div>
  );
}
