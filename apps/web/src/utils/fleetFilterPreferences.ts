// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export interface FleetFilterPreferences {
  includedTags: string[];
  excludedTags: string[];
  hiddenStatuses: string[];
}

interface StoredFleetFilterPreferences extends FleetFilterPreferences {
  version: 1;
}

export const EMPTY_FLEET_FILTERS: FleetFilterPreferences = {
  includedTags: [],
  excludedTags: [],
  hiddenStatuses: [],
};

export const FLEET_STATUS_VALUES = ['UP', 'DOWN', 'PAUSED', 'STARTING', 'UNKNOWN'] as const;
export const FLEET_FILTER_PREFERENCES_KEY = 'fleet-filter-preferences';

const normalizeKey = (value: string) => value.toLocaleLowerCase();

function cleanStringArray(value: unknown) {
  if (!Array.isArray(value)) return [];

  const values = new Map<string, string>();
  for (const item of value) {
    if (typeof item !== 'string') continue;
    const cleaned = item.trim();
    if (cleaned) values.set(normalizeKey(cleaned), cleaned);
  }
  return [...values.values()];
}

export function parseFleetFilterPreferences(value: unknown): FleetFilterPreferences {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return EMPTY_FLEET_FILTERS;

  const record = value as Record<string, unknown>;
  if (record.version !== undefined && record.version !== 1) return EMPTY_FLEET_FILTERS;

  const excludedTags = cleanStringArray(record.excludedTags);
  const excludedKeys = new Set(excludedTags.map(normalizeKey));
  const includedTags = cleanStringArray(record.includedTags)
    .filter(tag => !excludedKeys.has(normalizeKey(tag)));
  const knownStatuses = new Set<string>(FLEET_STATUS_VALUES);
  const hiddenStatuses = [...new Set(
    cleanStringArray(record.hiddenStatuses)
      .map(status => status.toLocaleUpperCase())
      .filter(status => knownStatuses.has(status)),
  )];

  return { includedTags, excludedTags, hiddenStatuses };
}

export function parseStoredFleetFilterPreferences(
  serializedValue: string | null,
): FleetFilterPreferences | null {
  if (serializedValue === null) return null;
  try {
    return parseFleetFilterPreferences(JSON.parse(serializedValue));
  } catch {
    return EMPTY_FLEET_FILTERS;
  }
}

export function parseLegacyFleetFilterPreferences(
  serializedTags: string | null,
  serializedVisibleStatuses: string | null,
): FleetFilterPreferences {
  let includedTags: string[] = [];
  let visibleStatuses: string[] = [];

  try {
    includedTags = cleanStringArray(JSON.parse(serializedTags ?? '[]'));
    visibleStatuses = [...new Set(
      cleanStringArray(JSON.parse(serializedVisibleStatuses ?? '[]'))
        .map(status => status.toLocaleUpperCase())
        .filter(status => FLEET_STATUS_VALUES.includes(status as typeof FLEET_STATUS_VALUES[number])),
    )];
  } catch {
    return EMPTY_FLEET_FILTERS;
  }

  return {
    includedTags,
    excludedTags: [],
    hiddenStatuses: visibleStatuses.length > 0
      ? FLEET_STATUS_VALUES.filter(status => !visibleStatuses.includes(status))
      : [],
  };
}

export function serializeFleetFilterPreferences(
  preferences: FleetFilterPreferences,
): string {
  const stored: StoredFleetFilterPreferences = {
    version: 1,
    ...parseFleetFilterPreferences(preferences),
  };
  return JSON.stringify(stored);
}

export function resolveAvailableFleetFilters(
  preferences: FleetFilterPreferences,
  availableTags: string[],
  tagsReady: boolean,
) {
  if (!tagsReady) {
    return {
      applied: preferences,
      unavailableIncludedTags: [],
      unavailableExcludedTags: [],
    };
  }

  const availableKeys = new Set(availableTags.map(normalizeKey));
  const unavailableIncludedTags = preferences.includedTags
    .filter(tag => !availableKeys.has(normalizeKey(tag)));
  const unavailableExcludedTags = preferences.excludedTags
    .filter(tag => !availableKeys.has(normalizeKey(tag)));

  return {
    applied: {
      ...preferences,
      includedTags: preferences.includedTags
        .filter(tag => availableKeys.has(normalizeKey(tag))),
      excludedTags: preferences.excludedTags
        .filter(tag => availableKeys.has(normalizeKey(tag))),
    },
    unavailableIncludedTags,
    unavailableExcludedTags,
  };
}
