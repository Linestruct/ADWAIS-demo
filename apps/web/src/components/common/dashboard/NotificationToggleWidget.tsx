// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Bell, BellOff } from 'lucide-react';
import { useKiosk } from './KioskContext';

export function NotificationToggleWidget() {
  const { notificationsEnabled, toggleNotifications } = useKiosk();

  return (
    <button
      type="button"
      onClick={toggleNotifications}
      className={`relative overflow-hidden flex min-h-11 items-center justify-between px-4 rounded-full gap-4 transition-all duration-300 border-none whitespace-nowrap shrink-0 cursor-pointer font-bold
         ${notificationsEnabled 
          ? 'bg-secondary text-on-secondary hover:opacity-80'
          : 'bg-surface-container-lowest text-on-primary hover:bg-surface-container-high'
        }`}
      aria-label={notificationsEnabled ? 'Disable notifications' : 'Enable notifications'}
      title={notificationsEnabled ? 'Mute new order notifications' : 'Unmute new order notifications'}
    >
      <div className="flex items-center justify-center w-5 h-5 z-10">
        {notificationsEnabled ? (
          <Bell size={16} fill="currentColor" />
        ) : (
          <BellOff size={16} />
        )}
      </div>
    </button>
  );
}
