// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useState, type ReactNode } from 'react';
import { useNavigate, useRouterState } from '@tanstack/react-router';
import { KioskContext, type KioskMode } from './KioskContext';
import { useMediaQuery } from '../../../hooks/useMediaQuery';

const KIOSK_ROTATION_SECONDS = 15;
const IDLE_TIMEOUT_SECONDS = 10;
const KIOSK_ROUTES = ['/financial', '/fleet-status', '/intranet'] as const;
type KioskRoute = typeof KIOSK_ROUTES[number];

export function KioskProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const matches = useRouterState({ select: (s) => s.matches });
  const isMobileView = useMediaQuery('(max-width: 767px)');
  
  const [mode, setMode] = useState<KioskMode>('paused');
  const [kioskTimer, setKioskTimer] = useState(KIOSK_ROTATION_SECONDS);
  const [idleTimer, setIdleTimer] = useState(0);
  const [notificationsEnabled, setNotificationsEnabled] = useState(true);
  
  const currentRoute = matches[matches.length - 1]?.routeId;
  const isKioskEligibleRoute = KIOSK_ROUTES.includes(currentRoute as KioskRoute);
  const isKioskEnabled = !isMobileView && isKioskEligibleRoute;

  useEffect(() => {
    if (!isKioskEnabled) return;

    const ticker = setInterval(() => {
      if (currentRoute?.startsWith('/settings') || currentRoute === '/kiosk') {
        if (mode !== 'paused') {
           setMode('paused');
        }
        return;
      }
      
      if (mode === 'kiosk') {
        setKioskTimer((prev) => {
          if (prev <= 1) {
            const currentIndex = KIOSK_ROUTES.indexOf(currentRoute as KioskRoute);
            const nextIndex = (currentIndex + 1) % KIOSK_ROUTES.length;
            const nextRoute = KIOSK_ROUTES[nextIndex];
            
            if (nextRoute === '/financial') {
              void navigate({ to: '/financial' });
            } else if (nextRoute === '/fleet-status') {
              void navigate({ to: '/fleet-status' });
            } else if (nextRoute === '/intranet') {
              void navigate({ to: '/intranet' });
            }
            return KIOSK_ROTATION_SECONDS;
          }
          return prev - 1;
        });
      } else if (mode === 'interactive') {
        setIdleTimer((prev) => {
          if (prev >= IDLE_TIMEOUT_SECONDS) {
            setMode('kiosk');
            setKioskTimer(KIOSK_ROTATION_SECONDS);
            return 0;
          }
          return prev + 1;
        });
      }
    }, 1000);

    return () => clearInterval(ticker);
  }, [isKioskEnabled, mode, currentRoute, navigate]);

  useEffect(() => {
    if (!isKioskEnabled) return;

    const handleInteraction = () => {
      if (mode === 'kiosk') {
        setMode('interactive');
        setKioskTimer(KIOSK_ROTATION_SECONDS);
      }
      if (mode === 'interactive') {
        setIdleTimer(0);
      }
    };

    window.addEventListener('mousemove', handleInteraction);
    window.addEventListener('keydown', handleInteraction);
    window.addEventListener('touchstart', handleInteraction);

    return () => {
      window.removeEventListener('mousemove', handleInteraction);
      window.removeEventListener('keydown', handleInteraction);
      window.removeEventListener('touchstart', handleInteraction);
    };
  }, [isKioskEnabled, mode]);

  const togglePaused = () => {
    if (mode === 'paused') {
      setMode('kiosk');
      setKioskTimer(KIOSK_ROTATION_SECONDS);
    } else {
      setMode('paused');
      setKioskTimer(KIOSK_ROTATION_SECONDS);
      setIdleTimer(0);
    }
  };

  const effectiveMode = isKioskEnabled ? mode : 'interactive';
  const effectiveKioskTimer = isKioskEnabled ? kioskTimer : 0;
  const effectiveIdleTimer = isKioskEnabled ? idleTimer : 0;
  const progress = !isKioskEnabled
    ? 0
    : mode === 'kiosk'
    ? ((KIOSK_ROTATION_SECONDS - kioskTimer) / KIOSK_ROTATION_SECONDS) * 100 
    : mode === 'interactive' 
      ? (idleTimer / IDLE_TIMEOUT_SECONDS) * 100 
      : 0;

  const toggleNotifications = () => setNotificationsEnabled(prev => !prev);

  return (
    <KioskContext.Provider value={{ mode: effectiveMode, setMode, kioskTimer: effectiveKioskTimer, idleTimer: effectiveIdleTimer, progress, togglePaused, notificationsEnabled, toggleNotifications }}>
      {children}
    </KioskContext.Provider>
  );
}
