// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearch } from '@tanstack/react-router';
import type { UptimeMonitorDto } from '@types';
import type { Timeframe } from '../schemas';
import { normalizeStatus } from '../utils/monitorStatusHelper';
import {
  filterFleetMonitors,
  getFleetTags,
  isFleetSelectionVisible,
  type FleetSelection,
} from '../utils/fleetFilters';
import {
  EMPTY_FLEET_FILTERS,
  FLEET_FILTER_PREFERENCES_KEY,
  parseLegacyFleetFilterPreferences,
  parseStoredFleetFilterPreferences,
  resolveAvailableFleetFilters,
  serializeFleetFilterPreferences,
  type FleetFilterPreferences,
} from '../utils/fleetFilterPreferences';
import { useFleetAnalytics, useFleetAvailability, useFleetMonitors } from './useFleetQueries';

function normalizedPreferences(preferences: FleetFilterPreferences) {
  return {
    includedTags: [...preferences.includedTags].sort(),
    excludedTags: [...preferences.excludedTags].sort(),
    hiddenStatuses: [...preferences.hiddenStatuses].sort(),
  };
}

function filtersEqual(left: FleetFilterPreferences, right: FleetFilterPreferences) {
  return JSON.stringify(normalizedPreferences(left)) === JSON.stringify(normalizedPreferences(right));
}

function hasFilters(filters: FleetFilterPreferences) {
  return filters.includedTags.length > 0
    || filters.excludedTags.length > 0
    || filters.hiddenStatuses.length > 0;
}

function useStoredFleetFilterPreferences() {
  const [storedValue, setStoredValue] = useState<FleetFilterPreferences>(() => {
    if (typeof window === 'undefined') return EMPTY_FLEET_FILTERS;

    const stored = parseStoredFleetFilterPreferences(
      window.localStorage.getItem(FLEET_FILTER_PREFERENCES_KEY),
    );
    return stored ?? parseLegacyFleetFilterPreferences(
      window.localStorage.getItem('fleet-filter-tags'),
      window.localStorage.getItem('fleet-filter-statuses'),
    );
  });

  const setValue = useCallback((
    value: FleetFilterPreferences | ((current: FleetFilterPreferences) => FleetFilterPreferences),
  ) => {
    setStoredValue(current => value instanceof Function ? value(current) : value);
  }, []);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    try {
      window.localStorage.setItem(
        FLEET_FILTER_PREFERENCES_KEY,
        serializeFleetFilterPreferences(storedValue),
      );
      window.localStorage.removeItem('fleet-filter-tags');
      window.localStorage.removeItem('fleet-filter-statuses');
    } catch (error) {
      console.error(error);
    }
  }, [storedValue]);

  return [storedValue, setValue] as const;
}

export function useFleetStatusViewModel() {
  const search = useSearch({ from: '/fleet-status' }) as {
    timeframe: Timeframe;
    tenantId?: string;
    monitorId?: number;
  };
  const navigate = useNavigate({ from: '/fleet-status' });
  const timeframe = search.timeframe;
  const selectionIntent: FleetSelection | null = search.tenantId
    ? { tenantId: search.tenantId, monitorId: search.monitorId ?? null }
    : null;
  const setSelection = useCallback((nextSelection: FleetSelection | null) => {
    void navigate({
      search: previous => ({
        ...previous,
        tenantId: nextSelection?.tenantId,
        monitorId: nextSelection?.monitorId ?? undefined,
      }),
    });
  }, [navigate]);

  const [savedFilters, setSavedFilters] = useStoredFleetFilterPreferences();
  const [activeFilters, setActiveFilters] = useState<FleetFilterPreferences>(() => savedFilters);

  const clearSelection = () => setSelection(null);
  const clearActiveFilters = () => setActiveFilters(EMPTY_FLEET_FILTERS);
  const saveFilterPreferences = () => setSavedFilters(activeFilters);
  const restoreSavedFilters = () => setActiveFilters(savedFilters);
  const forgetSavedFilters = () => {
    setSavedFilters(EMPTY_FLEET_FILTERS);
    setActiveFilters(EMPTY_FLEET_FILTERS);
  };

  const globalMonitorsQuery = useFleetMonitors(timeframe);
  const allMonitorsInSystem = useMemo(() => globalMonitorsQuery.data ?? [], [globalMonitorsQuery.data]);
  const availableTags = useMemo(() => getFleetTags(allMonitorsInSystem), [allMonitorsInSystem]);
  const resolvedFilters = useMemo(
    () => resolveAvailableFleetFilters(
      activeFilters,
      availableTags,
      globalMonitorsQuery.isSuccess,
    ),
    [activeFilters, availableTags, globalMonitorsQuery.isSuccess],
  );
  const appliedFilters = resolvedFilters.applied;
  const filteredMonitors = useMemo(
    () => filterFleetMonitors(allMonitorsInSystem, appliedFilters),
    [allMonitorsInSystem, appliedFilters],
  );

  const selection = selectionIntent && (
    globalMonitorsQuery.isLoading || isFleetSelectionVisible(allMonitorsInSystem, selectionIntent)
  )
    ? selectionIntent
    : null;

  const goBack = () => {
    if (selection?.monitorId != null) {
      setSelection({ tenantId: selection.tenantId, monitorId: null });
      return;
    }
    clearSelection();
  };

  const queryFilters = selection?.monitorId != null
    ? undefined
    : {
        tags: appliedFilters.includedTags.length > 0 ? appliedFilters.includedTags : undefined,
        excludedTags: appliedFilters.excludedTags.length > 0 ? appliedFilters.excludedTags : undefined,
        excludedStatuses: appliedFilters.hiddenStatuses.length > 0 ? appliedFilters.hiddenStatuses : undefined,
      };
  const analyticsQuery = useFleetAnalytics(
    timeframe,
    selection?.tenantId,
    selection?.monitorId,
    undefined,
    queryFilters,
  );
  const availabilityQuery = useFleetAvailability(
    timeframe,
    selection?.tenantId,
    selection?.monitorId,
    undefined,
    queryFilters,
  );

  const tenantMonitors = useMemo(
    () => selection
      ? filteredMonitors.filter(monitor => monitor.tenantId === selection.tenantId)
      : filteredMonitors,
    [filteredMonitors, selection],
  );
  const selectedMonitor = selection?.monitorId
    ? allMonitorsInSystem.find(monitor =>
        monitor.tenantId === selection.tenantId && monitor.id === selection.monitorId,
      ) ?? null
    : null;
  const scopedMonitors = useMemo(
    () => selectedMonitor ? [selectedMonitor] : tenantMonitors,
    [selectedMonitor, tenantMonitors],
  );
  const kpis = analyticsQuery.data?.kpis;

  const fleetStats = useMemo(() => {
    const enabled = scopedMonitors.filter(monitor => monitor.uptimeMonitorEnabled);
    const down = enabled.filter(monitor => {
      const status = normalizeStatus(monitor.currentStatus);
      return status === 'DOWN' || status === 'CRITICAL';
    });
    const degraded = enabled.filter(monitor => {
      const status = normalizeStatus(monitor.currentStatus);
      const isUp = ['UP', 'PAUSED', 'STARTING', 'UNKNOWN'].includes(status);
      const floor = monitor.latencyDegradedFloor;
      return isUp
        && monitor.currentLatency != null
        && floor != null
        && monitor.currentLatency > floor;
    });

    return {
      total: scopedMonitors.length,
      enabled,
      highestLatency: kpis?.highestLatency ?? 0,
      lowestLatency: kpis?.lowestLatency ?? 0,
      avgLatency: kpis?.averageLatency ?? 0,
      down,
      degraded,
      avgUptime: kpis?.averageUptime !== undefined ? kpis.averageUptime : null,
      uptimeGrowth: kpis?.uptimeGrowthPercentage ?? null,
      latencyGrowth: kpis?.latencyGrowthPercentage ?? null,
      highestLatencyGrowth: kpis?.highestLatencyGrowthPercentage ?? null,
      lowestLatencyGrowth: kpis?.lowestLatencyGrowthPercentage ?? null,
    };
  }, [kpis, scopedMonitors]);

  const handleMonitorSelect = (monitor: UptimeMonitorDto) => {
    if (!selection) {
      const isOnlyMonitorForTenant = allMonitorsInSystem
        .filter(candidate => candidate.tenantId === monitor.tenantId)
        .length === 1;
      setSelection({
        tenantId: monitor.tenantId,
        monitorId: isOnlyMonitorForTenant ? monitor.id : null,
      });
      return;
    }

    setSelection(monitor.id === selection.monitorId
      ? { ...selection, monitorId: null }
      : { tenantId: monitor.tenantId, monitorId: monitor.id });
  };

  const selectedTenantMonitor = selection?.tenantId
    ? allMonitorsInSystem.find(monitor => monitor.tenantId === selection.tenantId)
    : undefined;
  const selectedTenantName = selectedTenantMonitor
    ? selectedTenantMonitor.tenantName || selectedTenantMonitor.name?.split('-')[0]?.trim() || 'Tenant'
    : null;
  const activeScopeName = selectedMonitor
    ? `${selectedTenantName} - ${selectedMonitor.name}`
    : selection?.tenantId
      ? `${selectedTenantName} (Tenant Avg)`
      : 'Global (Fleet Avg)';

  return {
    selection,
    setSelection,
    clearSelection,
    goBack,
    clearActiveFilters,
    saveFilterPreferences,
    restoreSavedFilters,
    forgetSavedFilters,
    analyticsQuery,
    availabilityQuery,
    globalMonitorsQuery,
    tenantMonitors,
    scopedMonitors,
    fleetStats,
    handleMonitorSelect,
    selectedTenantName,
    selectedMonitor,
    activeScopeName,
    allMonitors: allMonitorsInSystem,
    availableTags,
    filteredMonitors,
    activeFilters,
    appliedFilters,
    unavailableIncludedTags: resolvedFilters.unavailableIncludedTags,
    unavailableExcludedTags: resolvedFilters.unavailableExcludedTags,
    setIncludedTags: (includedTags: string[]) => setActiveFilters(current => ({
      ...current,
      includedTags,
    })),
    setExcludedTags: (excludedTags: string[]) => setActiveFilters(current => ({
      ...current,
      excludedTags,
    })),
    setHiddenStatuses: (hiddenStatuses: string[]) => setActiveFilters(current => ({
      ...current,
      hiddenStatuses,
    })),
    hasActiveFilters: hasFilters(activeFilters),
    hasSavedFilters: hasFilters(savedFilters),
    hasUnsavedFilterChanges: !filtersEqual(activeFilters, savedFilters),
  };
}
