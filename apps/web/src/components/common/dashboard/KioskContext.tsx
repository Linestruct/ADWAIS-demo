// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createContext, useContext } from 'react';

export type KioskMode = 'kiosk' | 'interactive' | 'paused';

export interface KioskContextType {
  mode: KioskMode;
  setMode: (mode: KioskMode) => void;
  kioskTimer: number;
  idleTimer: number;
  progress: number;
  togglePaused: () => void;
  notificationsEnabled: boolean;
  toggleNotifications: () => void;
}

export const KioskContext = createContext<KioskContextType | null>(null);

export function useKiosk() {
  const context = useContext(KioskContext);
  if (!context) {
    return {
      mode: 'interactive' as KioskMode,
      setMode: () => {},
      kioskTimer: 0,
      idleTimer: 0,
      progress: 0,
      togglePaused: () => {},
      notificationsEnabled: true,
      toggleNotifications: () => {}
    };
  }
  return context;
}
