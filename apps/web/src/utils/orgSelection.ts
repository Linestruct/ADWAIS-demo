// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

export const ORG_SELECTION_KEY = 'adwais.selectedOrgId';
export const ORG_SELECTION_CHECK_EVENT = 'adwais:org-selection-check';

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

/**
 * Signals that a request was denied while an organization selection is
 * active. The watcher re-validates the selection against the reachable
 * organizations and resets it only if it is no longer valid.
 */
export function notifyOrgSelectionCheck(): void {
  window.dispatchEvent(new Event(ORG_SELECTION_CHECK_EVENT));
}

let currentOrgId: string | null = readStoredOrgId();
const listeners = new Set<() => void>();

/**
 * Single source of truth for the active organization selection. Every
 * `useOrgSelection` instance subscribes to this store, so a switch made in
 * the picker propagates to every page and query hook immediately.
 */
export function getSelectedOrgId(): string | null {
  return currentOrgId;
}

export function setSelectedOrgId(orgId: string | null): void {
  if (orgId === currentOrgId) return;
  currentOrgId = orgId;
  writeStoredOrgId(orgId);
  listeners.forEach(listener => listener());
}

export function subscribeOrgSelection(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}