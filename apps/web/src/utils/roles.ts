// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

export function isAdminRole(role: string | null | undefined): boolean {
  return role === 'Admin' || role === 'PlatformAdmin';
}

export function isStaffRole(role: string | null | undefined): boolean {
  return role === 'Admin' || role === 'Employee' || role === 'PlatformAdmin';
}

export function isKioskOrStaff(role: string | null | undefined): boolean {
  return role === 'Admin' || role === 'Employee' || role === 'Viewer' || role === 'PlatformAdmin';
}