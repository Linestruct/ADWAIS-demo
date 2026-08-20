// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {useEffect} from 'react';

export function useVisualViewportCssVars() {
  useEffect(() => {
    const root = document.documentElement;
    const visualViewport = window.visualViewport;

    const updateViewportVars = () => {
      const width = visualViewport?.width ?? window.innerWidth;
      const height = visualViewport?.height ?? window.innerHeight;

      root.style.setProperty('--app-viewport-width', `${width}px`);
      root.style.setProperty('--app-viewport-height', `${height}px`);
    };

    updateViewportVars();

    window.addEventListener('resize', updateViewportVars);
    window.addEventListener('orientationchange', updateViewportVars);
    visualViewport?.addEventListener('resize', updateViewportVars);
    visualViewport?.addEventListener('scroll', updateViewportVars);

    return () => {
      window.removeEventListener('resize', updateViewportVars);
      window.removeEventListener('orientationchange', updateViewportVars);
      visualViewport?.removeEventListener('resize', updateViewportVars);
      visualViewport?.removeEventListener('scroll', updateViewportVars);
    };
  }, []);
}
