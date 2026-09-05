// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Play, Pause } from 'lucide-react';
import { useKiosk } from './KioskContext';

export function KioskControls() {
  const { mode, progress, togglePaused } = useKiosk();

  return (
    <button
      type="button"
      onClick={togglePaused}
      className={`relative overflow-hidden flex gap-6 min-h-11 items-center justify-between min-w-[220px] px-5 border rounded-full transition-all duration-300 whitespace-nowrap shrink-0 cursor-pointer
        ${mode === 'kiosk' ? 'bg-emerald-50 border-emerald-200 hover:bg-emerald-100' :
          mode === 'interactive' ? 'bg-amber-50 border-amber-200 hover:bg-amber-100' :
          'bg-surface-container-low border-outline-variant hover:bg-surface-container-high'}`}
      title={mode === 'paused' ? 'Resume Kiosk' : 'Pause Kiosk'}
    >
      {/* Left side: Status dot and label matching UserAccountLink text-sm font-bold */}
      <div className="flex items-center flex-grow flex-1 gap-3">
        <div className={`w-3 h-3 rounded-full transition-colors duration-500
            ${mode === 'kiosk' ? 'bg-emerald-500 kiosk-pulse' :
            mode === 'interactive' ? 'bg-amber-500' : 'bg-slate-400'}`}
        />
        <span className={`text-base font-bold tracking-wider transition-colors duration-500
            ${mode === 'kiosk' ? 'text-emerald-700' :
            mode === 'interactive' ? 'text-amber-700' : 'text-slate-650'}`}>
          {mode.toUpperCase()}
        </span>
      </div>

      {/* Right side: Clear, sharp indicator icon matching font height */}
      <div className={`flex items-center justify-center transition-colors duration-500 z-10
        ${mode === 'kiosk' ? 'text-emerald-700' :
          mode === 'interactive' ? 'text-amber-700' : 'text-slate-650'}`}
      >
        {mode === 'paused' ? (
          <Play size={16} fill="currentColor" className="ml-0.5" />
        ) : (
          <Pause size={16} fill="currentColor" />
        )}
      </div>

      {/* Bottom Linear Progress Bar */}
      <div className="absolute bottom-0 left-0 right-0 h-[3px] bg-black/5 z-0">
        <div 
          className={`h-full transition-all duration-1000 ease-linear
            ${mode === 'kiosk' ? 'bg-emerald-500' :
              mode === 'interactive' ? 'bg-amber-500' : 'bg-transparent'}`}
          style={{ width: `${progress}%` }}
        />
      </div>
    </button>
  );
}
