// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export function countActiveFilterGroups(...groups: boolean[]) {
  return groups.reduce((count, active) => count + Number(active), 0);
}
