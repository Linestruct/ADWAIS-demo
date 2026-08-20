// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export const UPTIME_MONITOR_TYPES = [
  'HTTP',
  'KEYWORD',
  'PING',
  'PORT',
  'HEARTBEAT',
  'DNS',
] as const;

export const DEFAULT_UPTIME_MONITOR_TYPE = 'HTTP';

export function getMonitorType(type?: string | null): string {
  return type?.trim().toUpperCase() || DEFAULT_UPTIME_MONITOR_TYPE;
}
