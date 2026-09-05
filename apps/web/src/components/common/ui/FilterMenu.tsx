// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
} from 'react';
import { createPortal } from 'react-dom';
import { Check, Filter, ListRestart } from 'lucide-react';

type FilterMenuPlacement = 'top' | 'bottom' | 'auto';
type FilterMenuAlign = 'start' | 'end';

interface MenuPosition {
  left: number;
  top: number;
  width: number;
  maxHeight: number;
  opensAbove: boolean;
}

export interface FloatingFilterMenuProps {
  activeCount: number;
  ariaLabel: string;
  clearLabel: string;
  onClearAll: () => void;
  width: number;
  renderPanel: (floatingStyle: CSSProperties) => ReactNode;
  placement?: FilterMenuPlacement;
  align?: FilterMenuAlign;
  compact?: boolean;
  triggerClassName?: string;
}

export function FilterSectionHeader({ label, active, onClear }: {
  label: string;
  active: boolean;
  onClear: () => void;
}) {
  return (
    <div className="flex min-h-9 items-center justify-between gap-3">
      <span className="text-sm font-black uppercase tracking-wide text-on-surface-variant md:text-base">{label}</span>
      <button
        type="button"
        disabled={!active}
        onClick={onClear}
        aria-label={`Clear ${label.toLowerCase()}`}
        className="min-h-9 cursor-pointer rounded-full bg-surface-container-low px-3 py-1 text-sm font-bold text-on-surface-variant transition-colors hover:bg-surface-container-high disabled:pointer-events-none disabled:opacity-0 md:text-base"
      >
        Clear
      </button>
    </div>
  );
}

export function FilterChip({ label, checked, disabled = false, onChange }: {
  label: string;
  checked: boolean;
  disabled?: boolean;
  onChange: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={checked}
      disabled={disabled}
      onClick={onChange}
      className={`inline-flex h-8 cursor-pointer items-center rounded-lg border text-sm font-medium transition-colors outline-none focus-visible:ring-2 focus-visible:ring-secondary/40 disabled:cursor-not-allowed disabled:border-transparent disabled:bg-on-surface/[0.10] disabled:text-on-surface/[0.38] md:text-base ${checked
        ? 'gap-2 border-transparent bg-secondary-container px-2 text-on-secondary-container hover:bg-secondary-container/80'
        : 'border-outline-variant bg-surface px-4 text-on-surface-variant hover:bg-surface-container-low'
      }`}
    >
      {checked && <Check aria-hidden="true" size={18} strokeWidth={2.5} className="shrink-0" />}
      {label}
    </button>
  );
}

export function FilterPanelFrame({ title, children, embedded = false, floatingStyle }: {
  title: string;
  children: ReactNode;
  embedded?: boolean;
  floatingStyle?: CSSProperties;
}) {
  return (
    <div
      style={floatingStyle}
      className={embedded
        ? 'flex flex-col gap-4 p-4'
        : 'custom-scrollbar fixed z-[200] flex flex-col gap-4 overflow-y-auto rounded-3xl border border-outline-variant bg-surface p-4 m3-elevation-4'}
    >
      <div className="flex items-center gap-4 border-b border-outline-variant pb-3">
        <h2 className="m-0 text-base font-black text-on-surface">{title}</h2>
      </div>
      {children}
    </div>
  );
}

function resolveOpensAbove(
  placement: FilterMenuPlacement,
  spaceAbove: number,
  spaceBelow: number,
) {
  if (placement === 'top') return true;
  if (placement === 'bottom') return false;
  return spaceAbove >= spaceBelow;
}

export function FloatingFilterMenu({
  activeCount,
  ariaLabel,
  clearLabel,
  onClearAll,
  width: preferredWidth,
  renderPanel,
  placement = 'auto',
  align = 'start',
  compact = false,
  triggerClassName = '',
}: FloatingFilterMenuProps) {
  const controlsRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState<MenuPosition | null>(null);

  const updateMenuPosition = useCallback(() => {
    const trigger = triggerRef.current;
    if (!trigger) return;

    const viewportMargin = 16;
    const menuGap = 12;
    const rect = (controlsRef.current ?? trigger).getBoundingClientRect();
    const width = Math.min(preferredWidth, window.innerWidth - viewportMargin * 2);
    const preferredLeft = align === 'end' ? rect.right - width : rect.left;
    const left = Math.max(
      viewportMargin,
      Math.min(preferredLeft, window.innerWidth - width - viewportMargin),
    );
    const spaceAbove = rect.top - viewportMargin - menuGap;
    const spaceBelow = window.innerHeight - rect.bottom - viewportMargin - menuGap;
    const opensAbove = resolveOpensAbove(placement, spaceAbove, spaceBelow);

    setMenuPosition({
      left,
      top: opensAbove ? rect.top - menuGap : rect.bottom + menuGap,
      width,
      maxHeight: Math.max(0, opensAbove ? spaceAbove : spaceBelow),
      opensAbove,
    });
  }, [align, placement, preferredWidth]);

  useEffect(() => {
    if (!isOpen) return;

    panelRef.current
      ?.querySelector<HTMLElement>(
        'button:not([disabled]):not([tabindex="-1"]):not([aria-hidden="true"]), [href]:not([tabindex="-1"]), input:not([disabled]):not([tabindex="-1"]):not([aria-hidden="true"]), select:not([disabled]):not([tabindex="-1"]):not([aria-hidden="true"]), textarea:not([disabled]):not([tabindex="-1"]):not([aria-hidden="true"]), [tabindex]:not([tabindex="-1"]):not([aria-hidden="true"])',
      )
      ?.focus();

    const close = (restoreFocus = false) => {
      setIsOpen(false);
      if (restoreFocus) triggerRef.current?.focus();
    };
    const handlePointerDown = (event: PointerEvent) => {
      const target = event.target;
      if (controlsRef.current?.contains(target as Node)) return;
      if (panelRef.current?.contains(target as Node)) return;
      if (target instanceof Element && target.closest('[data-select-menu]')) return;
      close();
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return;
      if (document.querySelector('[data-select-menu]')) return;
      close(true);
    };

    document.addEventListener('pointerdown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    window.addEventListener('resize', updateMenuPosition);
    window.addEventListener('scroll', updateMenuPosition, true);
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('resize', updateMenuPosition);
      window.removeEventListener('scroll', updateMenuPosition, true);
    };
  }, [isOpen, updateMenuPosition]);

  const toggleMenu = () => {
    if (!isOpen) updateMenuPosition();
    setIsOpen(open => !open);
  };

  if (compact) {
    return (
      <div
        ref={controlsRef}
        role="group"
        aria-label={ariaLabel}
        className="relative flex shrink-0 items-center gap-1"
      >
        <button
          ref={triggerRef}
          type="button"
          onClick={toggleMenu}
          aria-expanded={isOpen}
          aria-label={activeCount > 0 ? `Filters, ${activeCount} active` : 'Filters'}
          className={`flex h-11 cursor-pointer items-center gap-2 rounded-l-[18px] rounded-r-[4px] px-4 text-base font-bold text-on-surface outline-none transition-colors focus-visible:ring-2 focus-visible:ring-secondary ${
            triggerClassName || 'bg-surface hover:bg-surface-container'
          }`}
        >
          <Filter aria-hidden="true" size={18} strokeWidth={2.5} />
          Filters
          {activeCount > 0 && (
            <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-secondary-container px-1 text-sm text-on-secondary-container">
              {activeCount}
            </span>
          )}
        </button>
        <button
          type="button"
          onClick={onClearAll}
          disabled={activeCount === 0}
          aria-label={clearLabel}
          title={clearLabel}
          className="flex h-11 w-11 cursor-pointer items-center justify-center rounded-l-[4px] rounded-r-[18px] bg-error-container text-on-error-container outline-none transition-colors enabled:hover:bg-error/20 focus-visible:ring-2 focus-visible:ring-secondary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38]"
        >
          <ListRestart aria-hidden="true" size={18} strokeWidth={2.5} />
        </button>
        {isOpen && menuPosition && createPortal(
          <div ref={panelRef}>
            {renderPanel({
              left: menuPosition.left,
              top: menuPosition.top,
              width: menuPosition.width,
              maxHeight: menuPosition.maxHeight,
              transform: menuPosition.opensAbove ? 'translateY(-100%)' : undefined,
            })}
          </div>,
          document.body,
        )}
      </div>
    );
  }

  return (
    <div
      ref={controlsRef}
      role="group"
      aria-label={ariaLabel}
      className="relative flex min-h-14 items-center gap-1 overflow-hidden bg-transparent p-1"
    >
      <button
        ref={triggerRef}
        type="button"
        onClick={toggleMenu}
        aria-expanded={isOpen}
        aria-label={activeCount > 0 ? `Filters, ${activeCount} active` : 'Filters'}
        className="flex h-14 cursor-pointer items-center justify-center gap-2 rounded-l-[28px] rounded-r-[8px] bg-secondary px-5 py-2 text-base font-bold text-on-secondary outline-none transition-colors m3-elevation-1 hover:bg-secondary/90 hover:m3-elevation-2 focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-secondary"
      >
        <Filter aria-hidden="true" size={18}/>
        Filters
        {activeCount > 0 && (
          <span className="flex h-6 min-w-6 items-center justify-center rounded-full bg-secondary-container px-2 text-sm text-on-secondary-container">
            {activeCount}
          </span>
        )}
      </button>
      <button
        type="button"
        onClick={onClearAll}
        disabled={activeCount === 0}
        aria-label={clearLabel}
        title={clearLabel}
        className="flex h-14 w-14 cursor-pointer items-center justify-center rounded-l-[8px] rounded-r-[28px] bg-error-container text-on-error-container enabled:m3-elevation-1 enabled:hover:bg-error/20 enabled:hover:m3-elevation-2 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-secondary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38]"
      >
        <ListRestart aria-hidden="true" size={18} className={"-translate-x-0.5"} />
      </button>
      {isOpen && menuPosition && createPortal(
        <div ref={panelRef}>
          {renderPanel({
            left: menuPosition.left,
            top: menuPosition.top,
            width: menuPosition.width,
            maxHeight: menuPosition.maxHeight,
            transform: menuPosition.opensAbove ? 'translateY(-100%)' : undefined,
          })}
        </div>,
        document.body,
      )}
    </div>
  );
}
