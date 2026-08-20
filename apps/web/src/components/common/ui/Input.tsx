// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React from 'react';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: React.ReactNode;
  containerClassName?: string;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, icon, className = '', disabled, containerClassName = '', ...props }, ref) => {
    const widthClass = containerClassName.includes('w-') ? '' : 'w-full';
    return (
      <div className={`flex min-w-0 flex-col gap-2 ${widthClass} ${containerClassName}`}>
        {label && (
          <label className="pl-1 text-sm font-bold text-on-surface-variant">
            {label}
          </label>
        )}
        <div className="relative flex items-center">
          {icon && (
            <div className="absolute left-3 text-on-surface-variant pointer-events-none flex items-center justify-center">
              {icon}
            </div>
          )}
          <input
            ref={ref}
            disabled={disabled}
            className={`min-h-12 w-full rounded-xl px-4 py-3 text-base font-medium text-on-surface outline-none transition-[background-color,color,box-shadow] placeholder:text-on-surface-variant focus:bg-primary-container focus:text-on-primary-container focus:ring-2 focus:ring-secondary/40 ${
              icon ? 'pl-9' : ''
            } ${
              disabled
                ? 'cursor-not-allowed bg-on-surface/[0.12] text-on-surface/[0.38]'
                : ''
            } ${
              error ? 'ring-2 ring-error/40 focus:bg-error-container focus:text-on-error-container focus:ring-error/40' : ''
            } ${className}`}
            {...props}
          />
        </div>
        {error && <span className="text-sm text-red-500 font-medium mt-0.5">{error}</span>}
      </div>
    );
  }
);

Input.displayName = 'Input';
