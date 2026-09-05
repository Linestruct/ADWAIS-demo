// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React, { forwardRef } from 'react';

export type ButtonVariant = 'filled' | 'tonal' | 'elevated' | 'outlined' | 'text';
export type ButtonColor = 'primary' | 'secondary' | 'tertiary' | 'error' | 'surface';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  color?: ButtonColor;
  icon?: React.ReactNode;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'filled', color = 'primary', icon, className = '', children, disabled, ...props }, ref) => {
    
    // Base styles
    const baseClasses = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-full px-5 text-sm font-bold transition-all focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 cursor-pointer';
    
    let variantClasses = '';

    // Color and Variant matrix
    if (variant === 'filled') {
      if (color === 'primary') variantClasses = 'bg-primary text-on-primary enabled:hover:bg-primary/90 focus-visible:outline-primary';
      else if (color === 'secondary') variantClasses = 'bg-secondary text-on-secondary enabled:hover:bg-secondary/90 focus-visible:outline-secondary';
      else if (color === 'error') variantClasses = 'bg-error text-on-error enabled:hover:bg-error/90 focus-visible:outline-error';
      else if (color === 'tertiary') variantClasses = 'bg-tertiary text-on-tertiary enabled:hover:bg-tertiary/90 focus-visible:outline-tertiary';
      else variantClasses = 'bg-surface text-on-surface enabled:hover:bg-surface/90 focus-visible:outline-surface';
    } else if (variant === 'tonal') {
      if (color === 'primary') variantClasses = 'bg-primary-container text-on-primary-container enabled:hover:m3-elevation-1 focus-visible:outline-primary';
      else if (color === 'secondary') variantClasses = 'bg-secondary-container text-on-secondary-container enabled:hover:m3-elevation-1 focus-visible:outline-secondary';
      else if (color === 'error') variantClasses = 'bg-error-container text-on-error-container enabled:hover:m3-elevation-1 focus-visible:outline-error';
      else if (color === 'tertiary') variantClasses = 'bg-tertiary-container text-on-tertiary-container enabled:hover:m3-elevation-1 focus-visible:outline-tertiary';
      else variantClasses = 'bg-surface-container text-on-surface enabled:hover:m3-elevation-1 focus-visible:outline-surface';
    } else if (variant === 'elevated') {
      const baseElevated = 'bg-surface-container-low m3-elevation-1 enabled:hover:m3-elevation-2';
      if (color === 'primary') variantClasses = `${baseElevated} text-primary focus-visible:outline-primary`;
      else if (color === 'secondary') variantClasses = `${baseElevated} text-secondary focus-visible:outline-secondary`;
      else if (color === 'error') variantClasses = `${baseElevated} text-error focus-visible:outline-error`;
      else if (color === 'tertiary') variantClasses = `${baseElevated} text-tertiary focus-visible:outline-tertiary`;
      else variantClasses = `${baseElevated} text-on-surface focus-visible:outline-surface`;
    } else if (variant === 'outlined') {
      const baseOutlined = 'border border-outline bg-transparent';
      if (color === 'primary') variantClasses = `${baseOutlined} text-primary enabled:hover:bg-primary/10 focus-visible:outline-primary`;
      else if (color === 'secondary') variantClasses = `${baseOutlined} text-secondary enabled:hover:bg-secondary/10 focus-visible:outline-secondary`;
      else if (color === 'error') variantClasses = `${baseOutlined} text-error enabled:hover:bg-error-container focus-visible:outline-error`;
      else if (color === 'tertiary') variantClasses = `${baseOutlined} text-tertiary enabled:hover:bg-tertiary/10 focus-visible:outline-tertiary`;
      else variantClasses = `${baseOutlined} text-on-surface enabled:hover:bg-on-surface/10 focus-visible:outline-surface`;
    } else if (variant === 'text') {
      const baseText = 'bg-transparent';
      if (color === 'primary') variantClasses = `${baseText} text-primary enabled:hover:bg-primary/10 focus-visible:outline-primary`;
      else if (color === 'secondary') variantClasses = `${baseText} text-secondary enabled:hover:bg-secondary/10 focus-visible:outline-secondary`;
      else if (color === 'error') variantClasses = `${baseText} text-error enabled:hover:bg-error-container focus-visible:outline-error`;
      else if (color === 'tertiary') variantClasses = `${baseText} text-tertiary enabled:hover:bg-tertiary/10 focus-visible:outline-tertiary`;
      else variantClasses = `${baseText} text-on-surface-variant enabled:hover:bg-on-surface/10 enabled:hover:text-on-surface focus-visible:outline-secondary`;
    }

    let disabledClasses = '';
    if (variant === 'filled' || variant === 'tonal' || variant === 'elevated') {
      disabledClasses = 'disabled:bg-on-surface/[0.12] disabled:text-on-surface/[0.38] disabled:shadow-none disabled:cursor-not-allowed';
    } else if (variant === 'outlined') {
      disabledClasses = 'disabled:border-on-surface/[0.12] disabled:text-on-surface/[0.38] disabled:bg-transparent disabled:cursor-not-allowed';
    } else if (variant === 'text') {
      disabledClasses = 'disabled:text-on-surface/[0.38] disabled:bg-transparent disabled:cursor-not-allowed';
    }

    const iconOnlyClasses = (!children && icon) ? '!w-11 !px-0 rounded-full flex items-center justify-center' : '';

    const combinedClasses = `${baseClasses} ${variantClasses} ${disabledClasses} ${iconOnlyClasses} ${className}`.trim();

    return (
      <button ref={ref} className={combinedClasses} disabled={disabled} {...props}>
        {icon && <span className="shrink-0 flex items-center">{icon}</span>}
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';
