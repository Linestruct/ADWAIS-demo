// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

export const ORG_SELECTION_KEY = 'adwais.selectedOrgId';
export const ORG_SELECTION_RESET_EVENT = 'adwais:org-selection-reset';

export function readStoredOrgId(): string | null {
  try {
    return sessionStorage.getItem(ORG_SELECTION_KEY);
  } catch {
    return null;
  }
}

export function writeStoredOrgId(orgId: string | null): void {
  try {
    if (orgId === null) {
      sessionStorage.removeItem(ORG_SELECTION_KEY);
    } else {
      sessionStorage.setItem(ORG_SELECTION_KEY, orgId);
    }
  } catch {
    // Storage unavailable; selection stays in memory for this session.
  }
}

export function notifyOrgSelectionReset(): void {
  window.dispatchEvent(new Event(ORG_SELECTION_RESET_EVENT));
}