// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createContext } from 'react';

export interface MobileFooterActionsSlots {
  panel: HTMLElement | null;
  indicator: HTMLElement | null;
  quickAction: HTMLElement | null;
}

export const MobileFooterActionsSlotContext = createContext<MobileFooterActionsSlots>({
  panel: null,
  indicator: null,
  quickAction: null,
});
