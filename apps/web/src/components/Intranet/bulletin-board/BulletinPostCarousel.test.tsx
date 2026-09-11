// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { act, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { BulletinPostCarousel } from './BulletinPostCarousel';

let resizeCallback: ResizeObserverCallback;

class ResizeObserverMock {
  constructor(callback: ResizeObserverCallback) {
    resizeCallback = callback;
  }

  observe() {}
  disconnect() {}
  unobserve() {}
}

describe('BulletinPostCarousel', () => {
  beforeEach(() => {
    vi.stubGlobal('ResizeObserver', ResizeObserverMock);
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => {
      callback(0);
      return 1;
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('uses two compact rows whenever its own height can accommodate them', () => {
    render(
      <BulletinPostCarousel>
        <div data-bulletin-post-card>One</div>
        <div data-bulletin-post-card>Two</div>
      </BulletinPostCarousel>,
    );
    const viewport = screen.getByLabelText('Bulletin board');
    Object.defineProperty(viewport, 'clientHeight', { configurable: true, value: 400 });

    act(() => resizeCallback([], {} as ResizeObserver));

    expect(viewport.style.gridTemplateRows).toMatch(/^repeat\(2,/);
  });

  it('keeps one full-height row when two usable cards would not fit', () => {
    render(
      <BulletinPostCarousel>
        <div data-bulletin-post-card>One</div>
      </BulletinPostCarousel>,
    );
    const viewport = screen.getByLabelText('Bulletin board');
    Object.defineProperty(viewport, 'clientHeight', { configurable: true, value: 300 });

    act(() => resizeCallback([], {} as ResizeObserver));

    expect(viewport.style.gridTemplateRows).toMatch(/^repeat\(1,/);
  });

  it('centers navigation controls on the card track instead of the full panel', () => {
    render(
      <BulletinPostCarousel>
        <div data-bulletin-post-card>One</div>
      </BulletinPostCarousel>,
    );
    const viewport = screen.getByLabelText('Bulletin board');
    Object.defineProperties(viewport, {
      clientHeight: { configurable: true, value: 300 },
      clientWidth: { configurable: true, value: 400 },
      scrollLeft: { configurable: true, value: 0, writable: true },
      scrollWidth: { configurable: true, value: 800 },
    });

    act(() => resizeCallback([], {} as ResizeObserver));

    const nextButton = screen.getByRole('button', { name: 'Next bulletin posts' });
    expect(nextButton).toHaveClass('absolute');
    expect(nextButton).toHaveStyle({ top: '117px' });
  });
});
