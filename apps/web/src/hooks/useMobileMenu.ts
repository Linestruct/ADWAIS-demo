// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {useState} from 'react';

export function useMobileMenu(pathname: string) {
  const [menuState, setMenuState] = useState({isOpen: false, pathname});
  const isOpen = menuState.isOpen && menuState.pathname === pathname;

  return {
    isOpen,
    close: () => setMenuState({isOpen: false, pathname}),
    toggle: () => setMenuState((current) => ({
      isOpen: current.pathname === pathname ? !current.isOpen : true,
      pathname,
    })),
  };
}
