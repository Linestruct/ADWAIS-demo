// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Database, Play, ShieldAlert } from 'lucide-react';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { SecureButton } from '../../common/ui/SecureButton';
import { DateTimePickerField } from '../../common/ui/DateTimePickerField';
import { ReadOnlyBanner } from '../../common/ui/ReadOnlyBanner';
import { Select } from '../../common/ui/Select';
import type { TenantResponseDto } from '@types';

interface ManualBackfillPanelProps {
  tenants: TenantResponseDto[] | undefined;
  isLoading: boolean;
  isError: boolean;
  triggerBackfill: {
    mutate: (variables: { tenantId: string; startDate: string; endDate: string }) => void;
    isPending: boolean;
  };
  disabled?: boolean;
}

export function ManualBackfillPanel({ tenants, isLoading, isError, triggerBackfill, disabled }: ManualBackfillPanelProps) {
  const [backfill, setBackfill] = useState({ tenantId: '', startDate: '', endDate: '' });
  const noTenants = !isLoading && !isError && tenants?.length === 0;
  const tenantSelectionDisabled = disabled || isLoading || isError || noTenants;

  const handleExecute = () => {
    if (disabled) return;
    triggerBackfill.mutate(backfill);
    setBackfill({ tenantId: '', startDate: '', endDate: '' });
  };

  return (
    <SettingsPanel 
      title="Historical Backfill"
      subtitle="Rebuild historical order data for a selected tenant and period."
      icon={<Database size={24} />}
    >
      {disabled && <ReadOnlyBanner message="You can review this form, but only administrators can run a backfill." />}
      <Select
            label="Tenant"
            value={backfill.tenantId}
            onChange={e => setBackfill({ ...backfill, tenantId: e.target.value })}
            disabled={tenantSelectionDisabled}
            size="lg"
        >
          <option value="" disabled>
            {isLoading ? 'Loading tenants...' : isError ? 'Unable to load tenants' : noTenants ? 'No tenants available' : 'Select a tenant...'}
          </option>
          {(tenants || []).map((t) => (
            <option key={t.id} value={t.id}>{t.name} ({t.type})</option>
          ))}
      </Select>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <DateTimePickerField
          id="backfill-start-date"
          label="Start Date"
          value={backfill.startDate}
          onChange={val => setBackfill(prev => ({ ...prev, startDate: val }))}
          size="compact"
          disabled={disabled}
        />
        <DateTimePickerField
          id="backfill-end-date"
          label="End Date"
          value={backfill.endDate}
          onChange={val => setBackfill(prev => ({ ...prev, endDate: val }))}
          size="compact"
          disabled={disabled}
        />
      </div>
      <div className="flex items-start gap-3 rounded-xl bg-error-container p-4 text-on-error-container">
        <ShieldAlert size={18} className="mt-1 shrink-0" aria-hidden="true" />
        <p className="mt-1 text-sm font-medium leading-5">
          Large backfills may take a few minutes to ingest due to rate-limits and may also temporarily slow reporting while monitoring views are rebuilt.
        </p>
      </div>
      <SecureButton
        onClick={handleExecute}
        locked={disabled}
        lockTitle="Requires Admin privileges"
        loading={triggerBackfill.isPending}
        loadingText="Initiating..."
        icon={<Play size={16} />}
        className="flex min-h-11 w-fit cursor-pointer items-center justify-center gap-2 rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
        disabled={!backfill.tenantId || isLoading || isError || noTenants}
      >
        Execute Backfill
      </SecureButton>
    </SettingsPanel>
  );
}
