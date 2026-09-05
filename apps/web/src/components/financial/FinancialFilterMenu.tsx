// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useMemo, type CSSProperties } from 'react';
import type { TenantType } from '@types';
import { countActiveFilterGroups } from '../../utils/filterCounts';
import {
  FilterChip,
  FilterPanelFrame,
  FilterSectionHeader,
  FloatingFilterMenu,
} from '../common/ui/FilterMenu';
import { Select } from '../common/ui/Select';

export interface FinancialTenantOption {
  id: string;
  name: string;
  type: TenantType;
}

export interface FinancialFilterProps {
  tenants: FinancialTenantOption[];
  selectedTenantId: string | null;
  selectedTypes: TenantType[];
  isLoading?: boolean;
  onTenantChange: (tenantId: string | null) => void;
  onTypesChange: (types: TenantType[]) => void;
  onClearAll: () => void;
}

const TYPE_OPTIONS: Array<{ label: string; value: TenantType }> = [
  { label: 'B2B', value: 'B2B' },
  { label: 'B2C', value: 'B2C' },
  { label: 'Mixed', value: 'Mixed' },
];

function activeFilterCount(selectedTenantId: string | null, selectedTypes: TenantType[]) {
  return countActiveFilterGroups(Boolean(selectedTenantId), selectedTypes.length > 0);
}

function toggleType(types: TenantType[], type: TenantType) {
  return types.includes(type) ? types.filter(value => value !== type) : [...types, type];
}

export function FinancialFilterPanel({
  tenants,
  selectedTenantId,
  selectedTypes,
  isLoading = false,
  onTenantChange,
  onTypesChange,
  embedded = false,
  floatingStyle,
}: FinancialFilterProps & { embedded?: boolean; floatingStyle?: CSSProperties }) {
  const selectedTenant = useMemo(
    () => tenants.find(tenant => tenant.id === selectedTenantId),
    [selectedTenantId, tenants],
  );
  const tenantOptions = useMemo(() => {
    if (selectedTypes.length === 0) return tenants;
    const selected = new Set<TenantType>(selectedTypes);
    return tenants.filter(tenant => selected.has(tenant.type));
  }, [selectedTypes, tenants]);
  const availableTypes = useMemo(
    () => new Set<TenantType>(tenants.map(tenant => tenant.type)),
    [tenants],
  );

  return (
    <FilterPanelFrame title="Filter financials" embedded={embedded} floatingStyle={floatingStyle}>
      <div>
        <FilterSectionHeader
          label="Tenant"
          active={Boolean(selectedTenantId)}
          onClear={() => onTenantChange(null)}
        />
        <Select
          aria-label="Tenant"
          value={selectedTenantId ?? ''}
          disabled={isLoading}
          onChange={event => onTenantChange(event.target.value || null)}
          variant="outlined"
          size="md"
          containerClassName="mt-1"
          className="md:text-base"
        >
          <option value="">Global portfolio</option>
          {tenantOptions.map(tenant => (
            <option key={tenant.id} value={tenant.id}>{tenant.name}</option>
          ))}
        </Select>
      </div>

      <div>
        <FilterSectionHeader label="Business model" active={selectedTypes.length > 0} onClear={() => onTypesChange([])} />
        <div className="mt-1 flex flex-wrap gap-2">
          {TYPE_OPTIONS.map(option => {
            const isSelected = selectedTypes.includes(option.value);
            const nextTypes = toggleType(selectedTypes, option.value);
            const invalidForTenant = Boolean(
              selectedTenant && nextTypes.length > 0 && !nextTypes.includes(selectedTenant.type),
            );
            return (
              <FilterChip
                key={option.value}
                label={option.label}
                checked={isSelected}
                disabled={!availableTypes.has(option.value) || invalidForTenant || Boolean(
                  selectedTenant && !isSelected && selectedTenant.type !== option.value,
                )}
                onChange={() => onTypesChange(nextTypes)}
              />
            );
          })}
        </div>
      </div>
    </FilterPanelFrame>
  );
}

export function FinancialFilterMenu(props: FinancialFilterProps) {
  const count = activeFilterCount(props.selectedTenantId, props.selectedTypes);

  return (
    <FloatingFilterMenu
      activeCount={count}
      ariaLabel="Financial filter controls"
      clearLabel="Clear all financial filters"
      onClearAll={props.onClearAll}
      width={420}
      placement="top"
      renderPanel={floatingStyle => <FinancialFilterPanel {...props} floatingStyle={floatingStyle} />}
    />
  );
}
