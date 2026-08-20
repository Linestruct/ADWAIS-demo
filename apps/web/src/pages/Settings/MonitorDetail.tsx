// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React, { useState } from 'react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { Activity, Save, X, Unlink, Trash2 } from 'lucide-react';
import { useTenantsViewModel } from '../../hooks/useTenantsViewModel';
import { useUpdateMonitorMutation, useControlMonitorMutation, useDeleteMonitorMutation } from '../../hooks/useMonitorQueries';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { FormField, CheckboxField } from '../../components/common/ui/FormField';
import { Button } from '../../components/common/ui/Button';
import { UPTIME_MONITOR_TYPES } from '../../utils/monitorTypeHelper';
import { getTenantFaviconUrl } from '../../utils/tenantHelper';
import type { UptimeMonitorDto } from '@types';

function MonitorDetailForm({ monitor, isAdmin, onBack }: { monitor: UptimeMonitorDto, isAdmin: boolean, onBack: () => void }) {
  const navigate = useNavigate();
  const updateMonitor = useUpdateMonitorMutation();
  const deleteMonitor = useDeleteMonitorMutation();
  const controlMonitor = useControlMonitorMutation();
  
  const getInitialDraft = (m: UptimeMonitorDto) => ({
    name: m.name || '',
    url: m.url || '',
    type: m.type || UPTIME_MONITOR_TYPES[0],
    uptimeSla: m.uptimeSla ? m.uptimeSla.toString() : '',
  });

  const [draft, setDraft] = useState(getInitialDraft(monitor));
  const [isUserEditing, setIsUserEditing] = useState(false);
  const [prevMonitor, setPrevMonitor] = useState(monitor);
  const [imgError, setImgError] = useState(false);

  const faviconUrl = monitor.tenantImageUrl || getTenantFaviconUrl(monitor.tenantBaseUrl || draft.url);
  const showIcon = !faviconUrl || imgError;

  // When background data refreshes, only update the draft if the user isn't actively editing
  if (monitor !== prevMonitor) {
    setPrevMonitor(monitor);
    if (!isUserEditing) {
      setDraft(getInitialDraft(monitor));
    }
  }

  const isDirty =
    draft.name !== (monitor.name || '') ||
    draft.url !== (monitor.url || '') ||
    draft.type !== (monitor.type || UPTIME_MONITOR_TYPES[0]) ||
    draft.uptimeSla !== (monitor.uptimeSla ? monitor.uptimeSla.toString() : '');

  const handleSave = () => {
    const payload: Partial<UptimeMonitorDto> = {};
    if (draft.name !== (monitor.name || '')) payload.name = draft.name;
    if (draft.url !== (monitor.url || '')) payload.url = draft.url;
    if (draft.type !== (monitor.type || UPTIME_MONITOR_TYPES[0])) payload.type = draft.type;
    
    const sla = draft.uptimeSla === '' ? null : parseFloat(draft.uptimeSla);
    if (sla !== monitor.uptimeSla) {
        payload.uptimeSla = sla;
    }

    updateMonitor.mutate(
      { id: monitor.id, payload },
      {
        onSuccess: () => {
          setIsUserEditing(false);
          setTimeout(() => {
            updateMonitor.reset();
          }, 3000);
        }
      }
    );
  };

  const handleCancel = () => {
    setIsUserEditing(false);
    setDraft(getInitialDraft(monitor));
  };

  const handleDelete = () => {
    if (confirm('Are you sure you want to delete this monitor?')) {
      if (monitor.id !== undefined) {
        deleteMonitor.mutate(monitor.id, {
          onSuccess: () => {
            void navigate({ to: '/settings/monitors' });
          }
        });
      }
    }
  };

  const updateDraft = (updates: Partial<typeof draft>) => {
    setIsUserEditing(true);
    setDraft(prev => ({ ...prev, ...updates }));
  };

  return (
    <>
        <SettingsPanelHeader
          title="Edit Monitor"
          subtitle={`Editing details for ${monitor.name}`}
          iconContainerClassName={!showIcon ? 'overflow-hidden bg-transparent' : undefined}
          icon={showIcon ? <Activity size={24} /> : <img src={faviconUrl!} alt={monitor.name || ''} className="h-full w-full object-cover" onError={() => setImgError(true)} />}
          onBack={onBack}
        >
            <div className="flex items-center gap-2">
              <Button
                onClick={handleDelete}
                disabled={!isAdmin || deleteMonitor.isPending}
                variant="text"
                color="error"
                icon={<Trash2 size={16} />}
              >
                Delete Monitor
              </Button>
              <Button
                onClick={handleCancel}
                disabled={!isAdmin || !isDirty || updateMonitor.isPending}
                variant="text"
                color="surface"
                icon={<X size={16} />}
              >
                Cancel
              </Button>
              <Button
                onClick={handleSave}
                disabled={!isAdmin || !isDirty || updateMonitor.isPending}
                variant="tonal"
                color="primary"
                icon={<Save size={16} />}
              >
                Save Changes
              </Button>
            </div>
        </SettingsPanelHeader>

      <div className="custom-scrollbar flex-1 overflow-y-auto px-6 py-6">
        <div className="grid grid-cols-1 items-start gap-10 xl:grid-cols-2">
          
          <div className="space-y-6">
            <h3 className="text-lg font-bold text-on-surface">Monitor Details</h3>
            <FormField
              id="monitor-name"
              label="Name"
              value={draft.name}
              onChange={(e) => updateDraft({ name: e.target.value })}
              disabled={!isAdmin || updateMonitor.isPending}
            />

            <FormField
              id="monitor-url"
              label="URL"
              type="url"
              value={draft.url}
              onChange={(e) => updateDraft({ url: e.target.value })}
              disabled={!isAdmin || updateMonitor.isPending}
            />

            <FormField
                as="select"
                id="monitor-type"
                label="Monitor Type"
                value={draft.type}
                onChange={e => updateDraft({ type: e.target.value })}
                disabled={!isAdmin || updateMonitor.isPending}
            >
                {UPTIME_MONITOR_TYPES.map(type => <option key={type} value={type}>{type}</option>)}
            </FormField>

            <FormField
              id="monitor-sla"
              label="Uptime SLA (hours)"
              type="number"
              step="0.1"
              value={draft.uptimeSla}
              onChange={(e) => updateDraft({ uptimeSla: e.target.value })}
              disabled={!isAdmin}
            />

            <CheckboxField
              id="monitor-enabled"
              label="Monitor Enabled"
              checked={monitor.uptimeMonitorEnabled ?? true}
              disabled={!isAdmin || controlMonitor.isPending}
              onChange={e => {
                  if (monitor.id !== undefined) {
                      controlMonitor.mutate({ id: monitor.id, action: e.target.checked ? 'start' : 'pause' });
                  }
              }}
            />
          </div>

          <MonitorAssignmentPanel monitor={monitor} isAdmin={isAdmin} />
        </div>
      </div>
    </>
  );
}

export const MonitorAssignmentPanel = React.memo(function MonitorAssignmentPanel({ monitor, isAdmin }: { monitor: UptimeMonitorDto, isAdmin: boolean }) {
  const navigate = useNavigate();
  const { tenants, assignMonitor, unassignMonitor } = useTenantsViewModel();
  const SYSTEM_TENANT_ID = '00000000-0000-0000-0000-000000000001';
  const isAssignedToRealTenant = monitor.tenantId != null && monitor.tenantId !== SYSTEM_TENANT_ID;
  const assignedTenant = isAssignedToRealTenant ? tenants?.find(t => t.id === monitor.tenantId) : null;
  const availableTenants = tenants?.filter(t => t.id !== SYSTEM_TENANT_ID) || [];

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-bold text-on-surface">Tenant Assignment</h3>
      
      {assignedTenant ? (
          <div className="flex items-center justify-between rounded-xl border border-outline-variant bg-surface p-4">
              <button
                  type="button"
                  className="min-w-0 cursor-pointer text-left transition-colors hover:text-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary"
                  onClick={() => void navigate({ to: '/settings/tenants/$tenantId', params: { tenantId: assignedTenant.id } })}
              >
                  <div className="text-xs font-bold uppercase tracking-wider text-on-surface-variant mb-1">Assigned Owner</div>
                  <div className="font-bold text-on-surface">{assignedTenant.name}</div>
                  <div className="text-sm text-on-surface-variant">{assignedTenant.orderProviderSettings?.endpointUrl}</div>
              </button>
                  <button
                      type="button"
                      onClick={(event) => {
                          event.stopPropagation();
                          if (monitor.id !== undefined) unassignMonitor.mutate(monitor.id);
                      }}
                      disabled={!isAdmin || unassignMonitor.isPending}
                      className="flex items-center gap-2 rounded-full px-3 py-1.5 text-sm font-bold text-error hover:bg-error-container hover:text-on-error-container transition-colors disabled:cursor-not-allowed disabled:text-on-surface/[0.38] disabled:hover:bg-transparent disabled:hover:text-on-surface/[0.38]"
                  >
                      <Unlink size={16} /> Unassign
                  </button>
          </div>
      ) : (
          <p className="text-sm text-on-surface-variant italic">This monitor is currently unassigned (system monitor).</p>
      )}

          <div className="pt-2">
              <FormField
                  as="select"
                  id="assign-tenant"
                  label={assignedTenant ? "Re-assign to another Tenant" : "Assign to Tenant"}
                  value=""
                  onChange={(e) => {
                      if (e.target.value && monitor.id !== undefined) {
                          assignMonitor.mutate({ id: monitor.id, tenantId: e.target.value });
                      }
                  }}
                  disabled={!isAdmin || assignMonitor.isPending}
              >
                  <option value="" disabled>Select a tenant...</option>
                  {availableTenants.map(t => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                  ))}
              </FormField>
          </div>
    </div>
  );
});

export function MonitorDetailView() {
  const navigate = useNavigate();
  const { monitorId } = useParams({ strict: false }) as { monitorId: string };
  const { allMonitors, isAdmin } = useTenantsViewModel();
  
  const monitorIdNumber = parseInt(monitorId, 10);
  const monitor = allMonitors.find(m => m.id === monitorIdNumber);

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      <SettingsPanel className="flex-1 max-h-none">
        {!monitor ? (
            <SettingsPanelHeader
                title="Edit Monitor"
                subtitle="Loading..."
                icon={<Activity size={24} />}
                onBack={() => void navigate({ to: '/settings/monitors' })}
            />
        ) : (
            <MonitorDetailForm key={monitor.id} monitor={monitor} isAdmin={isAdmin} onBack={() => void navigate({ to: '/settings/monitors' })} />
        )}
      </SettingsPanel>
    </div>
  );
}
