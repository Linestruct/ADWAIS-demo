// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, type ReactNode } from 'react';

const RIPPLE_SELECTOR = [
  'button:not(:disabled):not([data-md3-ripple="off"])',
  '[role="button"]:not([aria-disabled="true"]):not([data-md3-ripple="off"])',
  '[data-md3-ripple]:not([data-md3-ripple="off"]):not([aria-disabled="true"])',
].join(',');

const MIN_VISIBLE_MS = 90;

type RippleState = {
  element: HTMLElement;
  rippleSpan: HTMLSpanElement;
  startedAt: number;
  released: boolean;
};

function findRippleTarget(target: EventTarget | null): HTMLElement | null {
  return target instanceof Element ? target.closest<HTMLElement>(RIPPLE_SELECTOR) : null;
}

export function Md3RippleProvider({ children }: { children: ReactNode }) {
  useEffect(() => {
    const pointerRipples = new Map<number, RippleState>();
    const keyboardRipples = new Map<HTMLElement, RippleState>();

    const begin = (element: HTMLElement, clientX?: number, clientY?: number): RippleState => {
      const rect = element.getBoundingClientRect();
      const x = clientX == null ? rect.width / 2 : clientX - rect.left;
      const y = clientY == null ? rect.height / 2 : clientY - rect.top;
      const radius = Math.hypot(Math.max(x, rect.width - x), Math.max(y, rect.height - y));
      const size = radius * 2;

      const rippleSpan = document.createElement('span');
      rippleSpan.className = 'md3-ripple-span';
      rippleSpan.style.width = `${size}px`;
      rippleSpan.style.height = `${size}px`;
      rippleSpan.style.left = `${x - radius}px`;
      rippleSpan.style.top = `${y - radius}px`;
      rippleSpan.style.backgroundColor = getComputedStyle(element).color;

      element.appendChild(rippleSpan);

      rippleSpan.animate(
        [{ transform: 'scale(0)' }, { transform: 'scale(1)' }],
        { duration: 700, easing: 'cubic-bezier(0.4, 0, 0.2, 1)', fill: 'forwards' }
      );
      
      rippleSpan.animate(
        [{ opacity: 0 }, { opacity: 0.12 }],
        { duration: 90, easing: 'linear', fill: 'forwards' }
      );

      return {
        element,
        rippleSpan,
        startedAt: performance.now(),
        released: false,
      };
    };

    const finishRelease = (state: RippleState) => {
      const elapsed = performance.now() - state.startedAt;
      const delay = Math.max(0, MIN_VISIBLE_MS - elapsed);

      setTimeout(() => {
        if (!document.body.contains(state.rippleSpan)) return;
        const fade = state.rippleSpan.animate(
          [{ opacity: 0.12 }, { opacity: 0 }],
          { duration: 280, easing: 'cubic-bezier(0.4, 0, 0.2, 1)', fill: 'forwards' }
        );
        fade.onfinish = () => {
          if (state.rippleSpan.parentNode) {
            state.rippleSpan.remove();
          }
        };
      }, delay);
    };

    const release = (state: RippleState) => {
      if (state.released) return;
      state.released = true;
      finishRelease(state);
    };

    const handlePointerDown = (event: PointerEvent) => {
      if (event.pointerType === 'mouse' && event.button !== 0) return;
      const element = findRippleTarget(event.target);
      if (!element) return;
      
      const existing = pointerRipples.get(event.pointerId);
      if (existing) release(existing);
      
      pointerRipples.set(event.pointerId, begin(element, event.clientX, event.clientY));
    };

    const handlePointerRelease = (event: PointerEvent) => {
      const state = pointerRipples.get(event.pointerId);
      if (!state) return;
      pointerRipples.delete(event.pointerId);
      release(state);
    };

    const handlePointerOut = (event: PointerEvent) => {
      const state = pointerRipples.get(event.pointerId);
      if (!state || state.element.contains(event.relatedTarget as Node | null)) return;
      pointerRipples.delete(event.pointerId);
      release(state);
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.repeat || (event.key !== 'Enter' && event.key !== ' ')) return;
      const element = findRippleTarget(event.target);
      if (!element || keyboardRipples.has(element)) return;
      keyboardRipples.set(element, begin(element));
    };

    const releaseKeyboardRipple = (target: EventTarget | null) => {
      const element = findRippleTarget(target);
      if (!element) return;
      const state = keyboardRipples.get(element);
      if (!state) return;
      keyboardRipples.delete(element);
      release(state);
    };
    
    const handleKeyUp = (event: KeyboardEvent) => releaseKeyboardRipple(event.target);
    const handleFocusOut = (event: FocusEvent) => releaseKeyboardRipple(event.target);

    document.addEventListener('pointerdown', handlePointerDown);
    document.addEventListener('pointerup', handlePointerRelease);
    document.addEventListener('pointercancel', handlePointerRelease);
    document.addEventListener('pointerout', handlePointerOut);
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('keyup', handleKeyUp);
    document.addEventListener('focusout', handleFocusOut);

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown);
      document.removeEventListener('pointerup', handlePointerRelease);
      document.removeEventListener('pointercancel', handlePointerRelease);
      document.removeEventListener('pointerout', handlePointerOut);
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('keyup', handleKeyUp);
      document.removeEventListener('focusout', handleFocusOut);
      
      pointerRipples.forEach(release);
      keyboardRipples.forEach(release);
    };
  }, []);

  return children;
}
