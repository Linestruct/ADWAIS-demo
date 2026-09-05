// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { CSSProperties } from 'react';
import {
  FilterChip,
  FilterPanelFrame,
  FilterSectionHeader,
  FloatingFilterMenu,
} from '../../common/ui/FilterMenu';
import { Select } from '../../common/ui/Select';

type SortOrder = 'asc' | 'desc';

interface TenantFilters {
  token: string;
  fetch: string;
}

interface TenantSettingsFilterMenuProps {
  filters: TenantFilters;
  setFilters: (filters: TenantFilters) => void;
  sort: SortOrder;
  setSort: (sort: SortOrder) => void;
}

interface MonitorFilters {
  assignment: 'all' | 'assigned' | 'unassigned';
  tag: string;
  status: 'all' | 'enabled' | 'disabled';
  type: string;
}

interface MonitorSettingsFilterMenuProps {
  filters: MonitorFilters;
  setFilters: (filters: MonitorFilters) => void;
  sort: SortOrder;
  setSort: (sort: SortOrder) => void;
  tags: string[];
  types: string[];
}

function SortToggle({ value, onChange }: {
  value: SortOrder;
  onChange: (value: SortOrder) => void;
}) {
  return (
    <div className="grid grid-cols-2 rounded-full bg-surface-container" role="group" aria-label="Sort order">
      {([
        ['asc', 'A–Z'],
        ['desc', 'Z–A'],
      ] as const).map(([option, label]) => (
        <button
          key={option}
          type="button"
          aria-pressed={value === option}
          onClick={() => onChange(option)}
          className={`min-h-10 cursor-pointer rounded-full px-4 text-sm font-bold transition-colors outline-none focus-visible:ring-2 focus-visible:ring-secondary ${value === option
            ? 'bg-secondary text-on-secondary'
            : 'text-on-surface-variant hover:bg-surface-container-high hover:text-on-surface'
          }`}
        >
          {label}
        </button>
      ))}
    </div>
  );
}

function TenantFilterPanel({
  filters,
  setFilters,
  sort,
  setSort,
  floatingStyle,
}: TenantSettingsFilterMenuProps & { floatingStyle: CSSProperties }) {
  return (
    <FilterPanelFrame title="Filter tenants" floatingStyle={floatingStyle}>
      <div>
        <FilterSectionHeader
          label="Service token"
          active={filters.token !== 'all'}
          onClear={() => setFilters({ ...filters, token: 'all' })}
        />
        <div className="mt-1 flex flex-wrap gap-2">
          <FilterChip
            label="Set"
            checked={filters.token === 'set'}
            onChange={() => setFilters({ ...filters, token: filters.token === 'set' ? 'all' : 'set' })}
          />
          <FilterChip
            label="Missing"
            checked={filters.token === 'missing'}
            onChange={() => setFilters({ ...filters, token: filters.token === 'missing' ? 'all' : 'missing' })}
          />
        </div>
      </div>

      <div>
        <FilterSectionHeader
          label="Order fetching"
          active={filters.fetch !== 'all'}
          onClear={() => setFilters({ ...filters, fetch: 'all' })}
        />
        <div className="mt-1 flex flex-wrap gap-2">
          <FilterChip
            label="Enabled"
            checked={filters.fetch === 'on'}
            onChange={() => setFilters({ ...filters, fetch: filters.fetch === 'on' ? 'all' : 'on' })}
          />
          <FilterChip
            label="Disabled"
            checked={filters.fetch === 'off'}
            onChange={() => setFilters({ ...filters, fetch: filters.fetch === 'off' ? 'all' : 'off' })}
          />
        </div>
      </div>

      <div>
        <div className="flex min-h-9 items-center">
          <span className="text-sm font-black uppercase tracking-wide text-on-surface-variant md:text-base">Sort</span>
        </div>
        <SortToggle value={sort} onChange={setSort} />
      </div>
    </FilterPanelFrame>
  );
}

function MonitorFilterPanel({
  filters,
  setFilters,
  sort,
  setSort,
  tags,
  types,
  floatingStyle,
}: MonitorSettingsFilterMenuProps & { floatingStyle: CSSProperties }) {
  return (
    <FilterPanelFrame title="Filter monitors" floatingStyle={floatingStyle}>
      <div>
        <FilterSectionHeader
          label="Assignment"
          active={filters.assignment !== 'all'}
          onClear={() => setFilters({ ...filters, assignment: 'all' })}
        />
        <div className="mt-1 flex flex-wrap gap-2">
          <FilterChip
            label="Assigned"
            checked={filters.assignment === 'assigned'}
            onChange={() => setFilters({
              ...filters,
              assignment: filters.assignment === 'assigned' ? 'all' : 'assigned',
            })}
          />
          <FilterChip
            label="Unassigned"
            checked={filters.assignment === 'unassigned'}
            onChange={() => setFilters({
              ...filters,
              assignment: filters.assignment === 'unassigned' ? 'all' : 'unassigned',
            })}
          />
        </div>
      </div>

      <div>
        <FilterSectionHeader
          label="Status"
          active={filters.status !== 'all'}
          onClear={() => setFilters({ ...filters, status: 'all' })}
        />
        <div className="mt-1 flex flex-wrap gap-2">
          <FilterChip
            label="Enabled"
            checked={filters.status === 'enabled'}
            onChange={() => setFilters({
              ...filters,
              status: filters.status === 'enabled' ? 'all' : 'enabled',
            })}
          />
          <FilterChip
            label="Disabled"
            checked={filters.status === 'disabled'}
            onChange={() => setFilters({
              ...filters,
              status: filters.status === 'disabled' ? 'all' : 'disabled',
            })}
          />
        </div>
      </div>

      <div>
        <FilterSectionHeader
          label="Type"
          active={filters.type !== 'all'}
          onClear={() => setFilters({ ...filters, type: 'all' })}
        />
        <Select
          aria-label="Type"
          value={filters.type}
          onChange={event => setFilters({ ...filters, type: event.target.value })}
          variant="outlined"
          size="md"
          containerClassName="mt-1"
          className="md:text-base"
        >
          <option value="all">All types</option>
          {types.map(type => <option key={type} value={type}>{type}</option>)}
        </Select>
      </div>

      <div>
        <FilterSectionHeader
          label="Tag"
          active={filters.tag !== 'all'}
          onClear={() => setFilters({ ...filters, tag: 'all' })}
        />
        <Select
          aria-label="Tag"
          value={filters.tag}
          onChange={event => setFilters({ ...filters, tag: event.target.value })}
          variant="outlined"
          size="md"
          containerClassName="mt-1"
          className="md:text-base"
        >
          <option value="all">All tags</option>
          {tags.map(tag => <option key={tag} value={tag}>{tag}</option>)}
        </Select>
      </div>

      <div>
        <div className="flex min-h-9 items-center">
          <span className="text-sm font-black uppercase tracking-wide text-on-surface-variant md:text-base">Sort</span>
        </div>
        <SortToggle value={sort} onChange={setSort} />
      </div>
    </FilterPanelFrame>
  );
}

export function TenantSettingsFilterMenu(props: TenantSettingsFilterMenuProps) {
  const activeCount = Number(props.filters.token !== 'all') + Number(props.filters.fetch !== 'all');
  const clearAll = () => props.setFilters({ token: 'all', fetch: 'all' });

  return (
    <FloatingFilterMenu
      compact
      triggerClassName="bg-surface-container-high hover:bg-surface-container-highest"
      activeCount={activeCount}
      ariaLabel="Tenant filter controls"
      clearLabel="Clear all tenant filters"
      onClearAll={clearAll}
      width={320}
      placement="bottom"
      align="end"
      renderPanel={floatingStyle => <TenantFilterPanel {...props} floatingStyle={floatingStyle} />}
    />
  );
}

export function MonitorSettingsFilterMenu(props: MonitorSettingsFilterMenuProps) {
  const activeCount = Number(props.filters.assignment !== 'all') + Number(props.filters.tag !== 'all') + Number(props.filters.status !== 'all') + Number(props.filters.type !== 'all');
  const clearAll = () => props.setFilters({ assignment: 'all', tag: 'all', status: 'all', type: 'all' });

  return (
    <FloatingFilterMenu
      compact
      triggerClassName="bg-surface-container-high hover:bg-surface-container-highest"
      activeCount={activeCount}
      ariaLabel="Monitor filter controls"
      clearLabel="Clear all monitor filters"
      onClearAll={clearAll}
      width={320}
      placement="bottom"
      align="end"
      renderPanel={floatingStyle => <MonitorFilterPanel {...props} floatingStyle={floatingStyle} />}
    />
  );
}
