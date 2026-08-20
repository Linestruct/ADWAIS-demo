// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { CollectionPanel } from '../components/common/dashboard/CollectionPanel';
import { FleetMatrix } from '../components/FleetStatus/FleetMatrix';
import { NetworkLatencyChart } from '../components/FleetStatus/NetworkLatencyChart';
import { FleetSelectionPanel } from '../components/FleetStatus/FleetSelectionPanel';
import { DashboardLayout } from "../components/common/layout/DashboardLayout.tsx";
import { DashboardFlexRow } from "../components/common/layout/DashboardFlexRow.tsx";
import { DashboardFooter } from "../components/common/layout/DashboardFooter.tsx";
import { SyncStatusWidget } from '../components/common/dashboard/SyncStatusWidget';
import { PeriodSelector } from '../components/common/charts/PeriodSelector';
import { useFleetStatusViewModel } from "../hooks/useFleetStatusViewModel.ts";
import { EmptyState } from "../components/common/ui/EmptyState.tsx";
import { ErrorAlert } from '../components/common/ui/ErrorAlert';
import {
  FleetFilterMenu,
  FleetFilterPanel,
} from '../components/FleetStatus/FleetFilterMenu.tsx';
import { MobileFooterActions } from '../components/common/ui/MobileFooterActions.tsx';
import { ArrowLeft } from 'lucide-react';
import { countActiveFilterGroups } from '../utils/filterCounts.ts';
import { useNavigate } from '@tanstack/react-router';
import { getFleetSettingsTarget } from '../utils/fleetFilters.ts';

const EMPTY_LATENCY: never[] = [];

export function FleetStatus() {
  const vm = useFleetStatusViewModel();
  const navigate = useNavigate({ from: '/fleet-status' });
  const summarize = (values: string[]) => values.length <= 2
    ? values.join(', ')
    : `${values.slice(0, 2).join(', ')} +${values.length - 2}`;
  const matrixActions = (
    <div className="flex flex-wrap items-center justify-end gap-2">
      <button
        type="button"
        disabled={!vm.selection}
        onClick={vm.goBack}
        aria-label={vm.selection?.monitorId != null ? 'Back to tenant overview' : 'Back to fleet overview'}
        className="inline-flex min-h-9 cursor-pointer items-center gap-1.5 rounded-full border border-outline-variant bg-surface px-3 text-sm font-bold text-on-surface transition-colors hover:bg-surface-container focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-secondary disabled:cursor-not-allowed disabled:bg-on-surface/[0.10] disabled:text-on-surface/[0.38]"
      >
        <ArrowLeft size={16} aria-hidden="true" />
        <span>{vm.selection?.monitorId != null ? 'Tenant' : 'Fleet'}</span>
      </button>
      {vm.appliedFilters.includedTags.length > 0 && (
        <span className="rounded-full bg-secondary-container px-2.5 py-1 text-sm font-bold text-on-secondary-container">
          Including: {summarize(vm.appliedFilters.includedTags)}
        </span>
      )}
      {vm.appliedFilters.excludedTags.length > 0 && (
        <span className="rounded-full bg-error-container px-2.5 py-1 text-sm font-bold text-on-error-container">
          Excluding: {summarize(vm.appliedFilters.excludedTags)}
        </span>
      )}
      {vm.activeFilters.hiddenStatuses.length > 0 && (
        <span className="rounded-full bg-surface-container-high px-2.5 py-1 text-sm font-bold text-on-surface-variant">
          Hiding: {summarize(vm.activeFilters.hiddenStatuses)}
        </span>
      )}
      <span className="text-sm font-bold text-on-surface-variant">
        {vm.fleetStats.enabled.length} visible
      </span>
    </div>
  );

  const matrixContent = vm.globalMonitorsQuery.isError ? (
    <div className="p-4"><ErrorAlert title="Fleet status unavailable" message="Fleet status is temporarily unavailable." /></div>
  ) : vm.tenantMonitors.length === 0 ? (
    <div className="flex-1 flex items-center justify-center min-h-[200px]">
      <EmptyState
        variant="minimal"
        message={vm.selection ? "No monitors found for this tenant" : "No monitors found in the fleet"}
      />
    </div>
  ) : (
    <FleetMatrix
      monitors={vm.tenantMonitors}
      onMonitorSelect={vm.handleMonitorSelect}
      selectedMonitorId={vm.selection?.monitorId}
    />
  );

  const settingsTarget = getFleetSettingsTarget(vm.selection);
  const openSettings = () => void navigate(settingsTarget);

  return (
    <DashboardLayout>
      {/* Main Content Grid: Matrix on Left, selected scope & latency on Right */}
      <DashboardFlexRow weight="flex-1" gridCols="5" className="landscape-contained:min-h-0">

        {/* Right Column: Selected scope (Top) & Latency Chart (Bottom) (Takes 40% of width) */}
        <div className="landscape-lg:col-span-2 flex flex-col gap-4 min-h-[500px] contained:min-h-0 contained:h-full portrait-contained:contents">

          <FleetSelectionPanel
            selection={vm.selection}
            selectedTenantName={vm.selectedTenantName}
            selectedMonitor={vm.selectedMonitor}
            scopedMonitors={vm.scopedMonitors}
            availability={vm.availabilityQuery.data}
            isLoading={vm.availabilityQuery.isLoading}
            isError={vm.availabilityQuery.isError}
            isStale={vm.availabilityQuery.isPlaceholderData}
            averageLatency={vm.fleetStats.avgLatency}
            p10Latency={vm.fleetStats.lowestLatency}
            p90Latency={vm.fleetStats.highestLatency}
            uptimeGrowth={vm.fleetStats.uptimeGrowth}
            latencyGrowth={vm.fleetStats.latencyGrowth}
            p10LatencyGrowth={vm.fleetStats.lowestLatencyGrowth}
            p90LatencyGrowth={vm.fleetStats.highestLatencyGrowth}
            onOpenSettings={openSettings}
            settingsLabel={settingsTarget.label}
            className="flex-1 min-h-[350px] contained:min-h-0 max-h-[600px] xl:max-h-none"
          />

          <NetworkLatencyChart
            points={vm.analyticsQuery.data?.latencyPoints || EMPTY_LATENCY}
            title={`Latency: ${vm.activeScopeName}`}
            isLoading={vm.analyticsQuery.isLoading}
            isError={vm.analyticsQuery.isError}
            isStale={vm.analyticsQuery.isPlaceholderData}
            comparison="Preceding"
            className="flex-1 min-h-[350px] contained:min-h-0"
          />

        </div>

        {/* Left Column: Fleet Matrix (Takes 60% of width on landscape lg screens) */}
        <div className="landscape-lg:col-span-3 flex flex-col min-h-[500px] contained:min-h-0 contained:h-full">
          <CollectionPanel
              title={vm.selection ? `${vm.selectedTenantName} Monitors` : "Endpoint status – Click to select a tenant"}
              className="flex-grow min-h-[59px]"
              isLoading={vm.globalMonitorsQuery.isLoading}
              actions={matrixActions}
              titleClassName="!text-sm md:!text-base"
          >
            <div className="px-4 pb-4 h-full flex flex-col min-h-0">
              {matrixContent}
            </div>
          </CollectionPanel>
        </div>
      </DashboardFlexRow>

      {/* ── Footer Widgets (All Resolutions) ── */}
      <DashboardFooter>
        <div className="flex shrink-0 items-center">
          <SyncStatusWidget />
        </div>

        <div className="flex flex-wrap items-center justify-end gap-2">
          <FleetFilterMenu
            availableTags={vm.availableTags}
            includedTags={vm.activeFilters.includedTags}
            excludedTags={vm.activeFilters.excludedTags}
            unavailableIncludedTags={vm.unavailableIncludedTags}
            unavailableExcludedTags={vm.unavailableExcludedTags}
            hiddenStatuses={vm.activeFilters.hiddenStatuses}
            onIncludedTagsChange={vm.setIncludedTags}
            onExcludedTagsChange={vm.setExcludedTags}
            onHiddenStatusesChange={vm.setHiddenStatuses}
            onClearActive={vm.clearActiveFilters}
            onSaveDefault={vm.saveFilterPreferences}
            onRestoreSaved={vm.restoreSavedFilters}
            onForgetSaved={vm.forgetSavedFilters}
            hasSavedPreferences={vm.hasSavedFilters}
            hasUnsavedChanges={vm.hasUnsavedFilterChanges}
          />
          <div className="w-px h-6 bg-outline-variant mx-1 shrink-0" aria-hidden="true" />
          <PeriodSelector from="/fleet-status" />
        </div>
      </DashboardFooter>

      <MobileFooterActions
        activeCount={countActiveFilterGroups(
          vm.activeFilters.includedTags.length > 0 || vm.activeFilters.excludedTags.length > 0,
          vm.activeFilters.hiddenStatuses.length > 0,
        )}
        clearLabel="Clear active fleet filters"
        onClearAll={vm.clearActiveFilters}
      >
        <FleetFilterPanel
          embedded
          availableTags={vm.availableTags}
          includedTags={vm.activeFilters.includedTags}
          excludedTags={vm.activeFilters.excludedTags}
          unavailableIncludedTags={vm.unavailableIncludedTags}
          unavailableExcludedTags={vm.unavailableExcludedTags}
          hiddenStatuses={vm.activeFilters.hiddenStatuses}
          onIncludedTagsChange={vm.setIncludedTags}
          onExcludedTagsChange={vm.setExcludedTags}
          onHiddenStatusesChange={vm.setHiddenStatuses}
          onClearActive={vm.clearActiveFilters}
          onSaveDefault={vm.saveFilterPreferences}
          onRestoreSaved={vm.restoreSavedFilters}
          onForgetSaved={vm.forgetSavedFilters}
          hasSavedPreferences={vm.hasSavedFilters}
          hasUnsavedChanges={vm.hasUnsavedFilterChanges}
        />
      </MobileFooterActions>
    </DashboardLayout>
  );
}
