// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export function isAdminRole(role: string | null | undefined): boolean {
  return role === 'Admin' || role === 'PlatformAdmin';
}

export function isStaffRole(role: string | null | undefined): boolean {
  return role === 'Admin' || role === 'Employee' || role === 'PlatformAdmin';
}

export function isKioskOrStaff(role: string | null | undefined): boolean {
  return role === 'Admin' || role === 'Employee' || role === 'Viewer' || role === 'PlatformAdmin';
}