// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createContext } from 'react';

export interface RightSidebarSlot {
  container: HTMLElement | null;
}

export const RightSidebarSlotContext = createContext<RightSidebarSlot>({
  container: null,
});
