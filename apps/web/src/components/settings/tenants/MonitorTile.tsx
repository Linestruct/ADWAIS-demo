// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Play, Pause, X, Lock } from 'lucide-react';
import type { UptimeMonitorDto, TenantResponseDto, UpdateMonitorRequestDto } from '@types';
import { TileCard } from '../../common/layout/TileCard';
import { TileSaveBar } from '../../common/ui/TileSaveBar';
import { useUpdateMonitorMutation } from '../../../hooks/useMonitorQueries';
import { Input } from '../../common/ui/Input';
import { SecureButton } from '../../common/ui/SecureButton';
import { FormField } from '../../common/ui/FormField';
import { getMonitorType, UPTIME_MONITOR_TYPES } from '../../../utils/monitorTypeHelper';
import { getTagColor, getTagStyle } from "../../../utils/tagHelper.ts";

const SYSTEM_TENANT_ID = '00000000-0000-0000-0000-000000000001';

function normalizeStatus(status?: string | number): string {
  if (status === undefined || status === null) return 'UNKNOWN';
  const s = status.toString().toUpperCase().trim();
  if (s === '2' || s === 'UP') return 'UP';
  if (s === '8' || s === '9' || s === 'DOWN' || s === 'SEEMS DOWN' || s === 'CRITICAL') return 'DOWN';
  if (s === '0' || s === 'PAUSED') return 'PAUSED';
  return 'UNKNOWN';
}

interface MonitorTileProps {
  m: UptimeMonitorDto;
  toggleMonitor: {
    mutate: (variables: { id: number; action: 'start' | 'pause' }) => void;
  };
  assignMonitor: {
    mutate: (variables: { id: number; tenantId: string }) => void;
    isPending?: boolean;
  };
  unassignMonitor: {
    mutate: (id: number) => void;
    isPending?: boolean;
  };
  tenants: TenantResponseDto[] | undefined;
  isAdmin?: boolean;
}

export function MonitorTile({
  m,
  toggleMonitor,
  assignMonitor,
  unassignMonitor,
  tenants,
  isAdmin = false
}: MonitorTileProps) {
  const updateMonitor = useUpdateMonitorMutation();
  const isUnassigned = !m.tenantId || m.tenantId === SYSTEM_TENANT_ID;

  const [isAssigning, setIsAssigning] = useState(false);
  const [assignTenantId, setAssignTenantId] = useState('');

  const [draft, setDraft] = useState<{
    name: string;
    url: string;
    type: string;
    uptimeSla: number | '';
    tags: string[];
  }>({
    name: m.name || '',
    url: m.url || '',
    type: getMonitorType(m.type),
    uptimeSla: m.uptimeSla ?? '',
    tags: m.tags || []
  });

  const [tagInput, setTagInput] = useState('');

  const tagsEqual = (a: string[], b: string[]) => {
    if (a.length !== b.length) return false;
    return a.every((val, index) => val === b[index]);
  };

  const isDirty =
    draft.name !== (m.name || '') ||
    draft.url !== (m.url || '') ||
    draft.type !== getMonitorType(m.type) ||
    draft.uptimeSla !== (m.uptimeSla ?? '') ||
    !tagsEqual(draft.tags, m.tags || []);

  const handleSave = () => {
    const payload: UpdateMonitorRequestDto = {};
    if (draft.name !== (m.name || '')) payload.name = draft.name;
    if (draft.url !== (m.url || '')) payload.url = draft.url;
    if (draft.type !== getMonitorType(m.type)) payload.type = draft.type;
    if (draft.uptimeSla !== (m.uptimeSla ?? '')) {
      payload.sla = draft.uptimeSla === '' ? null : Number(draft.uptimeSla);
    }
    if (!tagsEqual(draft.tags, m.tags || [])) {
      payload.tags = draft.tags;
    }
    
    updateMonitor.mutate(
      { id: m.id, payload },
      {
        onSuccess: () => {
          setTimeout(() => {
            updateMonitor.reset();
          }, 3000);
        }
      }
    );
  };

  const handleCancel = () => {
    setDraft({
      name: m.name || '',
      url: m.url || '',
      type: getMonitorType(m.type),
      uptimeSla: m.uptimeSla ?? '',
      tags: m.tags || []
    });
    setTagInput('');
  };

  const header = (
    <>
      <div className="flex min-w-0 flex-1 items-center gap-3">
        <span className="relative flex h-2 w-2 shrink-0">
          {normalizeStatus(m.currentStatus) === 'UP' && <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-growth opacity-75"></span>}
          <span className={`relative inline-flex h-2 w-2 rounded-full ${normalizeStatus(m.currentStatus) === 'UP' ? 'bg-growth' : normalizeStatus(m.currentStatus) === 'DOWN' ? 'bg-decline' : 'bg-surface-container-highest'}`}></span>
        </span>
        <FormField
          label="Monitor name"
          hideLabel
          variant="outlined"
          density="compact"
          containerClassName="min-w-0 flex-1"
          value={draft.name}
          onChange={e => setDraft({...draft, name: e.target.value})}
          disabled={!isAdmin}
          className="truncate text-xl font-black"
          placeholder="Monitor Name"
        />
      </div>
    </>
  );

  const headerActions = !isAdmin ? (
    <span className="p-1.5 text-on-surface-variant opacity-60 cursor-not-allowed ml-2" title="Requires Admin privileges">
      <Lock size={14} />
    </span>
  ) : m.uptimeMonitorEnabled ? (
    <button aria-label={`Pause ${m.name || 'monitor'}`} onClick={() => toggleMonitor.mutate({ id: m.id, action: 'pause' })} className="flex h-10 w-10 cursor-pointer items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface focus-visible:outline focus-visible:outline-2 focus-visible:outline-secondary" title="Pause Monitor"><Pause size={18} /></button>
  ) : (
    <button aria-label={`Resume ${m.name || 'monitor'}`} onClick={() => toggleMonitor.mutate({ id: m.id, action: 'start' })} className="flex h-10 w-10 cursor-pointer items-center justify-center rounded-full text-growth transition-colors hover:bg-surface focus-visible:outline focus-visible:outline-2 focus-visible:outline-secondary" title="Resume Monitor"><Play size={18} /></button>
  );

  return (
    <TileCard header={header} headerActions={headerActions} isUnassigned={isUnassigned}>
      <FormField
        label="Target URL"
        value={draft.url}
        onChange={e => setDraft({ ...draft, url: e.target.value })}
        disabled={!isAdmin}
        variant="outlined"
        density="compact"
        placeholder="https://..."
      />

      <div className="mt-2 flex gap-3">
        <FormField
          as="select"
          label="Monitor Type"
          value={draft.type}
          onChange={e => setDraft({ ...draft, type: e.target.value })}
          disabled={!isAdmin}
          variant="outlined"
        
          density="compact"
          containerClassName="flex-1"
        >
          {!UPTIME_MONITOR_TYPES.includes(draft.type as typeof UPTIME_MONITOR_TYPES[number]) && (
            <option value={draft.type}>{draft.type}</option>
          )}
          {UPTIME_MONITOR_TYPES.map(type => <option key={type} value={type}>{type}</option>)}
        </FormField>
        <FormField
          label="Uptime SLA (%)"
          type="number"
          step="0.01"
          min="0"
          max="100"
          value={draft.uptimeSla}
          onChange={e => setDraft({ ...draft, uptimeSla: e.target.value === '' ? '' : Number(e.target.value) })}
          disabled={!isAdmin}
          variant="outlined"
        
          density="compact"
          containerClassName="min-w-0 flex-1"
          placeholder="e.g. 99.5"
        />
      </div>

      <div className="flex flex-col gap-2 mt-2">
        <label className="text-sm font-bold text-on-surface-variant uppercase tracking-wider">Tags</label>
        <div className="flex flex-wrap gap-2 mb-1.5 items-center">
          {draft.tags.map((tag) => {
            const color = getTagColor(tag);
            return (   
            <span
              key={tag}
              className={`flex items-center gap-1 rounded-full px-3 py-1 text-xs font-bold uppercase tracking-wider text-on-secondary-container ${getTagStyle(color)}`}
            >
              {tag}
              {isAdmin && (
                <button
                  type="button"
                  aria-label={`Remove ${tag} tag`}
                  onClick={() => setDraft({ ...draft, tags: draft.tags.filter(t => t !== tag) })}
                  className="ml-0.5 flex h-6 w-6 cursor-pointer items-center justify-center rounded-full hover:bg-surface hover:text-error focus-visible:outline focus-visible:outline-2 focus-visible:outline-error"
                >
                  <X size={14} />
                </button>
              )}
            </span>
            )
          })}
        </div>
        {isAdmin && (
          <FormField
            label="Add tag"
            hideLabel
            value={tagInput}
            onChange={e => setTagInput(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter') {
                e.preventDefault();
                const trimmed = tagInput.trim().toUpperCase();
                if (trimmed && !draft.tags.includes(trimmed)) {
                  setDraft({ ...draft, tags: [...draft.tags, trimmed] });
                  setTagInput('');
                }
              }
            }}
            variant="outlined"
        
            density="compact"
            placeholder="+ Add tag (Press Enter)"
          />
        )}
      </div>

      {isAdmin && (
        <TileSaveBar
          isDirty={isDirty}
          isPending={updateMonitor.isPending}
          isSuccess={updateMonitor.isSuccess}
          isError={updateMonitor.isError}
          errorMsg={updateMonitor.error ? (updateMonitor.error instanceof Error ? updateMonitor.error.message : String(updateMonitor.error)) : null}
          onSave={handleSave}
          onCancel={handleCancel}
        />
      )}

      <div className="flex items-center justify-between border-t border-outline pt-3">
        {!isAdmin ? (
          <div className="flex items-center justify-between w-full">
            <span className="text-sm font-bold text-on-surface-variant uppercase tracking-wide flex flex-col gap-1 min-w-0">
              Linked to
              <span className="text-sm text-on-surface-variant font-mono font-semibold truncate max-w-[250px] select-text cursor-text">
                {isUnassigned ? 'Unassigned' : ((tenants || []).find((t) => t.id === m.tenantId)?.name || m.tenantName || m.tenantId)}
              </span>
            </span>
            <span className="p-1 text-on-surface-variant opacity-60 cursor-not-allowed flex items-center gap-2" title="Requires Admin privileges">
              <Lock size={12} />
              <span className="text-sm font-bold uppercase tracking-wider text-on-surface-variant">Admin Only</span>
            </span>
          </div>
        ) : isUnassigned || isAssigning ? (
          <div className="flex items-center gap-4 w-full items-end">
            <Input
              list="assignable-tenants-list"
              value={assignTenantId}
              onChange={e => setAssignTenantId(e.target.value)}
              placeholder="Search for tenant..."
              containerClassName="flex-1"
              className="px-2 py-1.5 text-sm h-8 rounded-lg"
            />
            <SecureButton
              onClick={() => { 
                if(assignTenantId) {
                  assignMonitor.mutate({ id: m.id, tenantId: assignTenantId });
                  setIsAssigning(false);
                }
              }}
              disabled={!assignTenantId}
              loading={assignMonitor.isPending}
                className="flex min-h-10 shrink-0 cursor-pointer items-center justify-center rounded-full bg-on-primary-container px-4 text-sm font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]"
            >
              Link
            </SecureButton>
            <button type="button" aria-label="Cancel monitor assignment" onClick={() => { setIsAssigning(false); setAssignTenantId(''); }} className="flex h-10 w-10 shrink-0 cursor-pointer items-center justify-center rounded-full text-on-surface-variant hover:bg-surface"><X size={18}/></button>
          </div>
        ) : (
          <div className="flex items-center justify-between w-full">
            <span className="text-sm font-bold text-on-surface-variant uppercase tracking-wide flex flex-col gap-1 min-w-0">
              Linked to
              <span className="text-sm text-on-surface-variant font-mono font-semibold truncate max-w-[150px] select-text cursor-text">{(tenants || []).find((t) => t.id === m.tenantId)?.name || m.tenantName || m.tenantId}</span>
            </span>
            <div className="flex gap-4 ml-2">
              <SecureButton
                onClick={() => setIsAssigning(true)}
                locked={!isAdmin}
                className="flex min-h-10 cursor-pointer items-center justify-center gap-2 rounded-full px-4 text-sm font-bold text-on-surface transition-colors hover:bg-surface"
              >
                Reassign
              </SecureButton>
              <SecureButton
                onClick={() => { if(confirm('Unassign monitor?')) unassignMonitor.mutate(m.id); }}
                locked={!isAdmin}
                loading={unassignMonitor.isPending}
                className="flex min-h-10 cursor-pointer items-center justify-center gap-2 rounded-full px-4 text-sm font-bold text-error transition-colors hover:bg-error-container"
              >
                Unassign
              </SecureButton>
            </div>
          </div>
        )}
      </div>
    </TileCard>
  );
}
