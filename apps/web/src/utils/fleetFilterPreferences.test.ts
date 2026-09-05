// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { describe, expect, it } from 'vitest';
import {
  EMPTY_FLEET_FILTERS,
  parseFleetFilterPreferences,
  parseStoredFleetFilterPreferences,
  resolveAvailableFleetFilters,
  serializeFleetFilterPreferences,
} from './fleetFilterPreferences';

describe('fleet filter preferences', () => {
  it('validates, normalizes, and deduplicates persisted values without dependencies', () => {
    expect(parseFleetFilterPreferences({
      version: 1,
      includedTags: [' PROD ', 'prod', 'DEV', 42],
      excludedTags: ['dev'],
      hiddenStatuses: ['down', 'DOWN', 'retired'],
    })).toEqual({
      includedTags: ['prod'],
      excludedTags: ['dev'],
      hiddenStatuses: ['DOWN'],
    });
  });

  it('rejects malformed JSON and unknown schema versions', () => {
    expect(parseStoredFleetFilterPreferences('{broken')).toEqual(EMPTY_FLEET_FILTERS);
    expect(parseStoredFleetFilterPreferences(JSON.stringify({
      version: 99,
      includedTags: ['prod'],
    }))).toEqual(EMPTY_FLEET_FILTERS);
  });

  it('writes the current schema version while accepting the previous unversioned shape', () => {
    const preferences = parseStoredFleetFilterPreferences(JSON.stringify({
      includedTags: ['prod'],
      excludedTags: [],
      hiddenStatuses: [],
    }));

    expect(preferences?.includedTags).toEqual(['prod']);
    expect(JSON.parse(serializeFleetFilterPreferences(preferences!))).toMatchObject({
      version: 1,
      includedTags: ['prod'],
    });
  });

  it('does not reconcile during loading and reports unavailable tags after a successful load', () => {
    const preferences = {
      includedTags: ['prod', 'retired'],
      excludedTags: ['dev', 'removed'],
      hiddenStatuses: ['DOWN'],
    };

    expect(resolveAvailableFleetFilters(preferences, [], false).applied).toBe(preferences);
    expect(resolveAvailableFleetFilters(preferences, ['prod', 'dev'], true)).toEqual({
      applied: {
        includedTags: ['prod'],
        excludedTags: ['dev'],
        hiddenStatuses: ['DOWN'],
      },
      unavailableIncludedTags: ['retired'],
      unavailableExcludedTags: ['removed'],
    });
  });
});
