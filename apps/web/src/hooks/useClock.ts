// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState, useEffect } from 'react';

export function useClock(): Date {
  const [time, setTime] = useState(() => new Date());

  useEffect(() => {
    let intervalId: ReturnType<typeof setInterval>;

    // Calculate delay until the next minute boundary (seconds === 00)
    const msToNextMinute = 60000 - (new Date().getTime() % 60000);

    const timeoutId = setTimeout(() => {
      setTime(new Date());
      // Set interval to tick exactly on minute boundaries going forward
      intervalId = setInterval(() => {
        setTime(new Date());
      }, 60000);
    }, msToNextMinute);

    return () => {
      clearTimeout(timeoutId);
      if (intervalId) clearInterval(intervalId);
    };
  }, []);

  return time;
}
