// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {
  Children,
  forwardRef,
  isValidElement,
  useCallback,
  useEffect,
  useId,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from 'react';
import type {
  KeyboardEvent as ReactKeyboardEvent,
  OptionHTMLAttributes,
  ReactNode,
  Ref,
  SelectHTMLAttributes,
} from 'react';
import { createPortal } from 'react-dom';
import { Check, ChevronDown } from 'lucide-react';

export type SelectVariant = 'filled' | 'outlined' | 'pill' | 'brand' | 'plain';
export type SelectSize = 'xs' | 'sm' | 'md' | 'lg';

export interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'size'> {
  label?: string;
  hint?: string;
  error?: string;
  indicator?: ReactNode;
  variant?: SelectVariant;
  size?: SelectSize;
  fullWidth?: boolean;
  containerClassName?: string;
  triggerRef?: Ref<HTMLButtonElement>;
}

interface SelectOption {
  value: string;
  label: ReactNode;
  disabled: boolean;
}

interface MenuPosition {
  left: number;
  top: number;
  width: number;
  maxHeight: number;
  opensAbove: boolean;
}

const VIEWPORT_MARGIN = 8;
const MENU_GAP = 6;
const MIN_MENU_WIDTH = 160;
const MAX_MENU_HEIGHT = 320;

const variantClasses: Record<SelectVariant, string> = {
  filled: 'rounded-xl border-outline bg-surface-container-low text-on-surface hover:border-outline hover:bg-surface-container',
  outlined: 'rounded-xl border-outline bg-surface text-on-surface hover:border-outline hover:bg-surface-container-low',
  pill: 'rounded-full border-transparent bg-secondary text-on-secondary m3-elevation-1 hover:m3-elevation-2',
  brand: 'rounded-xl border-transparent bg-brand-btn-primary text-white shadow-sm hover:bg-brand-btn-primary/90 active:bg-brand-btn-primary/80',
  plain: 'rounded-md border-transparent bg-transparent text-inherit hover:bg-surface/20',
};

const sizeClasses: Record<SelectSize, string> = {
  xs: 'h-6 px-2 text-[11px] leading-none',
  sm: 'h-9 px-3 text-sm',
  md: 'h-10 px-3 text-sm',
  lg: 'h-12 px-4 text-base',
};

const indicatorColorClasses: Record<SelectVariant, string> = {
  filled: 'text-on-surface-variant',
  outlined: 'text-on-surface-variant',
  pill: 'text-on-secondary',
  brand: 'text-white/80',
  plain: 'text-current',
};

function assignRef<T>(ref: Ref<T> | undefined, value: T | null) {
  if (typeof ref === 'function') ref(value);
  else if (ref) ref.current = value;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select({
  label,
  hint,
  error,
  indicator = <ChevronDown size={16} strokeWidth={2.5} />,
  variant = 'filled',
  size = 'md',
  fullWidth = true,
  containerClassName = '',
  className = '',
  triggerRef: forwardedTriggerRef,
  id,
  value,
  defaultValue,
  disabled,
  onChange,
  onKeyDown,
  children,
  title,
  name,
  required,
  form,
  autoFocus,
  'aria-label': ariaLabel,
  'aria-labelledby': ariaLabelledBy,
  'aria-describedby': ariaDescribedBy,
  'aria-invalid': ariaInvalid,
  ...nativeProps
}, forwardedSelectRef) {
  const generatedId = useId();
  const selectId = id ?? `select-${generatedId}`;
  const listboxId = `${selectId}-listbox`;
  const descriptionId = hint || error ? `${selectId}-description` : undefined;
  const describedBy = [ariaDescribedBy, descriptionId].filter(Boolean).join(' ') || undefined;
  const nativeSelectRef = useRef<HTMLSelectElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState<MenuPosition | null>(null);
  const options = useMemo<SelectOption[]>(() => {
    const nextOptions: SelectOption[] = [];
    Children.forEach(children, child => {
      if (!isValidElement<OptionHTMLAttributes<HTMLOptionElement>>(child) || child.type !== 'option') return;
      nextOptions.push({
        value: String(child.props.value ?? child.props.children ?? ''),
        label: child.props.children,
        disabled: Boolean(child.props.disabled),
      });
    });
    return nextOptions;
  }, [children]);
  const [uncontrolledValue, setUncontrolledValue] = useState(() =>
    String(defaultValue ?? options[0]?.value ?? ''),
  );
  const selectedValue = value === undefined ? uncontrolledValue : String(value);
  const selectedIndex = Math.max(0, options.findIndex(option => option.value === selectedValue));
  const selectedOption = options[selectedIndex];
  const [highlightedIndex, setHighlightedIndex] = useState(selectedIndex);

  useImperativeHandle(forwardedSelectRef, () => nativeSelectRef.current as HTMLSelectElement);

  const setTriggerRef = useCallback((node: HTMLButtonElement | null) => {
    triggerRef.current = node;
    assignRef(forwardedTriggerRef, node);
  }, [forwardedTriggerRef]);

  const updateMenuPosition = useCallback(() => {
    const trigger = triggerRef.current;
    if (!trigger) return;

    const rect = trigger.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const width = Math.min(
      Math.max(rect.width, MIN_MENU_WIDTH),
      viewportWidth - VIEWPORT_MARGIN * 2,
    );
    const left = Math.max(
      VIEWPORT_MARGIN,
      Math.min(rect.left, viewportWidth - width - VIEWPORT_MARGIN),
    );
    const spaceBelow = viewportHeight - rect.bottom - VIEWPORT_MARGIN - MENU_GAP;
    const spaceAbove = rect.top - VIEWPORT_MARGIN - MENU_GAP;
    const opensAbove = spaceBelow < 160 && spaceAbove > spaceBelow;
    const availableHeight = opensAbove ? spaceAbove : spaceBelow;

    setMenuPosition({
      left,
      top: opensAbove ? rect.top - MENU_GAP : rect.bottom + MENU_GAP,
      width,
      maxHeight: Math.max(96, Math.min(MAX_MENU_HEIGHT, availableHeight)),
      opensAbove,
    });
  }, []);

  const openMenu = useCallback((index = selectedIndex) => {
    if (disabled) return;
    setHighlightedIndex(index);
    updateMenuPosition();
    setIsOpen(true);
  }, [disabled, selectedIndex, updateMenuPosition]);

  const closeMenu = useCallback((restoreFocus = false) => {
    setIsOpen(false);
    if (restoreFocus) triggerRef.current?.focus();
  }, []);

  useEffect(() => {
    if (autoFocus) triggerRef.current?.focus();
  }, [autoFocus]);

  useEffect(() => {
    if (!isOpen) return;

    const handlePointerDown = (event: PointerEvent) => {
      const target = event.target as Node;
      if (triggerRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      closeMenu();
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') closeMenu(true);
    };

    document.addEventListener('pointerdown', handlePointerDown, true);
    document.addEventListener('keydown', handleKeyDown);
    window.addEventListener('resize', updateMenuPosition);
    window.addEventListener('scroll', updateMenuPosition, true);
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown, true);
      document.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('resize', updateMenuPosition);
      window.removeEventListener('scroll', updateMenuPosition, true);
    };
  }, [closeMenu, isOpen, updateMenuPosition]);

  const selectOption = (option: SelectOption) => {
    if (option.disabled) return;
    if (value === undefined) setUncontrolledValue(option.value);

    const nativeSelect = nativeSelectRef.current;
    if (nativeSelect) {
      const valueSetter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'value')?.set;
      valueSetter?.call(nativeSelect, option.value);
      nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));
    }

    closeMenu(true);
  };

  const moveHighlight = (direction: 1 | -1) => {
    if (options.length === 0) return;
    let nextIndex = highlightedIndex;
    do {
      nextIndex = (nextIndex + direction + options.length) % options.length;
    } while (options[nextIndex]?.disabled && nextIndex !== highlightedIndex);
    setHighlightedIndex(nextIndex);
  };

  const handleTriggerKeyDown = (event: ReactKeyboardEvent<HTMLButtonElement>) => {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      if (!isOpen) openMenu(selectedIndex);
      else moveHighlight(event.key === 'ArrowDown' ? 1 : -1);
      return;
    }
    if (event.key === 'Home' && isOpen) {
      event.preventDefault();
      setHighlightedIndex(options.findIndex(option => !option.disabled));
      return;
    }
    if (event.key === 'End' && isOpen) {
      event.preventDefault();
      let lastEnabledIndex = options.length - 1;
      while (lastEnabledIndex >= 0 && options[lastEnabledIndex]?.disabled) {
        lastEnabledIndex -= 1;
      }
      setHighlightedIndex(lastEnabledIndex);
      return;
    }
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      if (isOpen && options[highlightedIndex]) selectOption(options[highlightedIndex]);
      else openMenu();
      return;
    }
    if (event.key === 'Escape' && isOpen) {
      event.preventDefault();
      closeMenu(true);
      return;
    }

    onKeyDown?.(event as unknown as ReactKeyboardEvent<HTMLSelectElement>);
  };

  return (
    <div className={`${fullWidth ? 'flex w-full' : 'inline-flex'} min-w-0 flex-col gap-1.5 ${containerClassName}`}>
      {label && (
        <label htmlFor={selectId} className="text-sm font-bold uppercase tracking-wider text-on-surface-variant">
          {label}
        </label>
      )}

      <select
        ref={nativeSelectRef}
        id={`${selectId}-native`}
        name={name}
        required={required}
        form={form}
        autoFocus={false}
        value={selectedValue}
        onChange={onChange}
        disabled={disabled}
        tabIndex={-1}
        aria-hidden="true"
        className="sr-only"
        {...nativeProps}
      >
        {children}
      </select>

      <button
        ref={setTriggerRef}
        id={selectId}
        type="button"
        role="combobox"
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-controls={listboxId}
        aria-activedescendant={isOpen ? `${listboxId}-option-${highlightedIndex}` : undefined}
        aria-invalid={error ? true : ariaInvalid}
        aria-label={ariaLabel}
        aria-labelledby={ariaLabelledBy}
        aria-describedby={describedBy}
        title={title}
        disabled={disabled}
        onClick={() => isOpen ? closeMenu() : openMenu()}
        onKeyDown={handleTriggerKeyDown}
        className={`${fullWidth ? 'w-full' : 'w-auto'} group min-w-0 cursor-pointer items-center gap-2 border font-semibold outline-none transition-[background-color,border-color,box-shadow] focus:border-brand-link focus:ring-2 focus:ring-brand-link/30 disabled:cursor-not-allowed disabled:border-transparent disabled:bg-on-surface/[0.12] disabled:text-on-surface/[0.38] ${indicator ? 'flex justify-between' : 'inline-flex'} ${sizeClasses[size]} ${variantClasses[variant]} ${className}`}
      >
        <span className="min-w-0 flex-1 truncate text-left">{selectedOption?.label ?? 'Select…'}</span>
        {indicator && (
          <span aria-hidden="true" className={`flex shrink-0 items-center justify-center ${indicatorColorClasses[variant]} group-disabled:text-on-surface/[0.38]`}>
            {indicator}
          </span>
        )}
      </button>

      {(error || hint) && (
        <p id={descriptionId} className={`m-0 text-sm ${error ? 'font-semibold text-error' : 'text-on-surface-variant'}`}>
          {error ?? hint}
        </p>
      )}

      {isOpen && menuPosition && createPortal(
        <div
          ref={menuRef}
          data-select-menu
          id={listboxId}
          role="listbox"
          aria-label={ariaLabel ?? label}
          className="fixed z-[200] flex flex-col gap-1 overflow-y-auto rounded-2xl border border-outline-variant bg-surface p-2 m3-elevation-3 custom-scrollbar"
          style={{
            left: menuPosition.left,
            top: menuPosition.top,
            width: menuPosition.width,
            maxHeight: menuPosition.maxHeight,
            transform: menuPosition.opensAbove ? 'translateY(-100%)' : undefined,
          }}
        >
          {options.map((option, index) => {
            const isSelected = option.value === selectedValue;
            const isHighlighted = index === highlightedIndex;
            return (
              <button
                key={`${option.value}-${index}`}
                id={`${listboxId}-option-${index}`}
                type="button"
                role="option"
                aria-selected={isSelected}
                disabled={option.disabled}
                onPointerMove={() => setHighlightedIndex(index)}
                onClick={() => selectOption(option)}
                className={`flex min-h-11 shrink-0 cursor-pointer items-center justify-between gap-3 rounded-xl px-3 py-2.5 text-left text-sm font-semibold outline-none transition-colors disabled:cursor-not-allowed disabled:opacity-40 ${isSelected
                  ? 'bg-secondary-container text-on-secondary-container'
                  : isHighlighted
                    ? 'bg-surface-container text-on-surface'
                    : 'text-on-surface hover:bg-surface-container-low'
                }`}
              >
                <span className="min-w-0 flex-1 truncate">{option.label}</span>
                {isSelected && <Check size={16} strokeWidth={2.5} className="shrink-0" />}
              </button>
            );
          })}
        </div>,
        document.body,
      )}
    </div>
  );
});
