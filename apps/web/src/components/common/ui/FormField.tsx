// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useId, type ChangeEventHandler, type InputHTMLAttributes, type ReactNode, type TextareaHTMLAttributes } from 'react';
import { Select, type SelectProps } from './Select';

export type FieldVariant = 'filled' | 'raised' | 'outlined' | 'plain';
export type FieldDensity = 'compact' | 'default';

interface FieldFrameProps {
  id: string;
  label: string;
  hideLabel?: boolean;
  helperText?: ReactNode;
  error?: ReactNode;
  meta?: ReactNode;
  containerClassName?: string;
  children: ReactNode;
}

interface SharedFieldProps {
  label: string;
  hideLabel?: boolean;
  helperText?: ReactNode;
  error?: ReactNode;
  meta?: ReactNode;
  variant?: FieldVariant;
  density?: FieldDensity;
  containerClassName?: string;
  className?: string;
}

export type InputFieldProps = SharedFieldProps & {
  as?: 'input';
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'size'>;

export type TextareaFieldProps = SharedFieldProps & {
  as: 'textarea';
} & TextareaHTMLAttributes<HTMLTextAreaElement>;

export type SelectFieldProps = SharedFieldProps & {
  as: 'select';
} & Omit<SelectProps, 'label' | 'hint' | 'error' | 'variant' | 'size' | 'containerClassName' | 'className'>;

export type FormFieldProps = InputFieldProps | TextareaFieldProps | SelectFieldProps;

interface CheckboxFieldProps {
  id?: string;
  label: string;
  checked: boolean;
  onChange: ChangeEventHandler<HTMLInputElement>;
  disabled?: boolean;
  helperText?: ReactNode;
  error?: ReactNode;
  className?: string;
}

const variantClasses: Record<FieldVariant, string> = {
  filled: 'border border-transparent bg-surface-container',
  raised: 'border border-transparent bg-surface-container-high',
  outlined: 'border border-outline bg-surface',
  plain: 'border border-transparent bg-transparent',
};

const densityClasses: Record<FieldDensity, string> = {
  compact: 'min-h-9 px-3 py-1.5 text-sm',
  default: 'min-h-12 px-4 py-3 text-base',
};

const sharedPropKeys = [
  'as',
  'label',
  'hideLabel',
  'helperText',
  'error',
  'meta',
  'variant',
  'density',
  'containerClassName',
  'className',
  'children',
] as const;

function getControlProps<T>(props: FormFieldProps): T {
  const controlProps = { ...props } as Record<string, unknown>;
  sharedPropKeys.forEach(key => {
    delete controlProps[key];
  });
  return controlProps as T;
}

function FieldFrame({
  id,
  label,
  hideLabel = false,
  helperText,
  error,
  meta,
  containerClassName = '',
  children,
}: FieldFrameProps) {
  return (
    <div className={`flex min-w-0 flex-col gap-2 ${containerClassName}`}>
      {!hideLabel && (
        <div className="flex min-h-5 items-start justify-between gap-3 pl-1">
          <label htmlFor={id} className="text-sm font-bold text-on-surface-variant">
            {label}
          </label>
          {meta && <span className="text-xs font-medium text-on-surface-variant">{meta}</span>}
        </div>
      )}
      {children}
      {(error || helperText) && (
        <div
          id={`${id}-${error ? 'error' : 'help'}`}
          className={`px-1 text-xs font-medium ${error ? 'text-error' : 'text-on-surface-variant'}`}
        >
          {error || helperText}
        </div>
      )}
    </div>
  );
}

export function FormField(props: FormFieldProps) {
  const generatedId = useId();
  const id = props.id || generatedId;
  const variant = props.variant ?? 'filled';
  const density = props.density ?? 'default';
  const describedBy = props.error
    ? `${id}-error`
    : props.helperText
      ? `${id}-help`
      : props['aria-describedby'];
  const controlClassName = `w-full rounded-xl font-medium text-on-surface outline-none transition-[background-color,border-color,color,box-shadow] placeholder:text-on-surface-variant focus:border-secondary focus:bg-primary-container focus:text-on-primary-container focus:ring-2 focus:ring-secondary/40 disabled:cursor-not-allowed disabled:border-transparent disabled:bg-on-surface/[0.12] disabled:text-on-surface/[0.38] ${variantClasses[variant]} ${densityClasses[density]} ${props.className || ''}`;
  const frameProps = {
    id,
    label: props.label,
    hideLabel: props.hideLabel,
    helperText: props.helperText,
    error: props.error,
    meta: props.meta,
    containerClassName: props.containerClassName,
  };

  const getWrapperClasses = (isTextarea: boolean) => {
    const base = `relative overflow-hidden flex ${
      isTextarea ? '' : 'items-center'
    } w-full rounded-xl font-medium text-on-surface transition-[background-color,border-color,color,box-shadow] focus-within:border-secondary focus-within:bg-primary-container focus-within:text-on-primary-container focus-within:ring-2 focus-within:ring-secondary/40 ${variantClasses[variant]} ${densityClasses[density]} ${props.className || ''}`;
    
    if (props.disabled) {
      return `${base} cursor-not-allowed !border-transparent !bg-on-surface/[0.12] !text-on-surface/[0.38]`;
    }
    return `${base} cursor-text`;
  };

  const handleWrapperClick = (e: React.MouseEvent<HTMLDivElement>) => {
    const el = e.currentTarget.querySelector<HTMLInputElement | HTMLTextAreaElement>('input, textarea');
    if (el && document.activeElement !== el) {
      el.focus();
    }
  };

  if (props.as === 'textarea') {
    const textareaProps = getControlProps<TextareaHTMLAttributes<HTMLTextAreaElement>>(props);
    return (
      <FieldFrame {...frameProps}>
        <div 
          className={getWrapperClasses(true)}
          data-md3-ripple={!props.disabled ? 'true' : undefined}
          onClick={handleWrapperClick}
        >
          <textarea
            {...textareaProps}
            id={id}
            aria-label={props.hideLabel ? props.label : props['aria-label']}
            aria-invalid={Boolean(props.error) || undefined}
            aria-describedby={describedBy}
            className="w-full h-full bg-transparent outline-none disabled:bg-transparent custom-scrollbar resize-y text-inherit placeholder:text-on-surface-variant"
          />
        </div>
      </FieldFrame>
    );
  }

  if (props.as === 'select') {
    const selectProps = getControlProps<SelectProps>(props);
    return (
      <FieldFrame {...frameProps}>
        <Select
          {...selectProps}
          id={id}
          aria-label={props.hideLabel ? props.label : props['aria-label']}
          aria-invalid={Boolean(props.error) || undefined}
          aria-describedby={describedBy}
          variant={variant === 'plain' ? 'plain' : variant === 'outlined' ? 'outlined' : 'filled'}
          size={density === 'compact' ? 'sm' : 'lg'}
          className={controlClassName}
        >
          {props.children}
        </Select>
      </FieldFrame>
    );
  }

  const inputProps = getControlProps<InputHTMLAttributes<HTMLInputElement>>(props);
  return (
    <FieldFrame {...frameProps}>
      <div 
        className={getWrapperClasses(false)}
        data-md3-ripple={!props.disabled ? 'true' : undefined}
        onClick={handleWrapperClick}
      >
        <input
          {...inputProps}
          id={id}
          aria-label={props.hideLabel ? props.label : props['aria-label']}
          aria-invalid={Boolean(props.error) || undefined}
          aria-describedby={describedBy}
          className="w-full h-full bg-transparent outline-none disabled:bg-transparent text-inherit placeholder:text-on-surface-variant"
        />
      </div>
    </FieldFrame>
  );
}

export function CheckboxField({
  id: suppliedId,
  label,
  checked,
  onChange,
  disabled = false,
  helperText,
  error,
  className = '',
}: CheckboxFieldProps) {
  const generatedId = useId();
  const id = suppliedId || generatedId;
  return (
    <div className={`flex min-w-0 flex-col gap-2 ${className}`}>
      <label
        htmlFor={id}
        className={`flex min-h-12 items-center gap-3 rounded-xl px-4 py-3 text-sm font-bold transition-colors ${
          disabled
            ? 'cursor-not-allowed bg-on-surface/[0.12] text-on-surface/[0.38]'
            : 'cursor-pointer bg-surface-container text-on-surface hover:bg-surface-container-high'
        }`}
      >
        <input
          id={id}
          type="checkbox"
          checked={checked}
          onChange={onChange}
          disabled={disabled}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={error ? `${id}-error` : helperText ? `${id}-help` : undefined}
          className="h-5 w-5 rounded border-outline-variant text-secondary focus:ring-2 focus:ring-secondary/40 disabled:cursor-not-allowed"
        />
        <span>{label}</span>
      </label>
      {(error || helperText) && (
        <span
          id={`${id}-${error ? 'error' : 'help'}`}
          className={`px-1 text-xs font-medium ${error ? 'text-error' : 'text-on-surface-variant'}`}
        >
          {error || helperText}
        </span>
      )}
    </div>
  );
}
