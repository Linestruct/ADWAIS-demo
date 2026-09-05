// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { Building2, Save, X, Trash2 } from 'lucide-react';
import { useTenantsViewModel } from '../../hooks/useTenantsViewModel';
import { useUpdateTenantMutation, useDeleteTenantMutation } from '../../hooks/useTenantQueries';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { FormField, CheckboxField } from '../../components/common/ui/FormField';
import { Button } from '../../components/common/ui/Button';
import { TenantMonitorsPanel } from '../../components/settings/tenants/TenantMonitorsPanel';
import { getTenantFaviconUrl } from '../../utils/tenantHelper';
import { useOrderProviderDescriptorsQuery } from '../../hooks/useIntegrationQueries';
import type { ProviderDescriptor, TenantResponseDto, UpdateTenantRequestDto } from '@types';

function TenantDetailForm({ tenant, providers, isAdmin, onBack }: { tenant: TenantResponseDto, providers: ProviderDescriptor[], isAdmin: boolean, onBack: () => void }) {
  const navigate = useNavigate();
  const updateTenant = useUpdateTenantMutation();
  const deleteTenant = useDeleteTenantMutation();
  const getInitialDraft = (t: TenantResponseDto) => ({
    name: t.name || '',
    orderProvider: t.orderProvider,
    settings: { ...(t.orderProviderSettings || {}) } as Record<string, string | null>,
    clearedSecretKeys: [] as string[],
    imageUrl: t.imageUrl || '',
    orderFetchingEnabled: t.orderFetchingEnabled ?? false,
  });

  const [draft, setDraft] = useState(getInitialDraft(tenant));
  const [isUserEditing, setIsUserEditing] = useState(false);
  const [prevTenant, setPrevTenant] = useState(tenant);
  const [imgError, setImgError] = useState(false);

  const selectedProvider = providers.find(provider => provider.id === draft.orderProvider);
  const settings = selectedProvider?.settings?.filter(setting => setting.key) ?? [];
  const providerSettingsComplete = !draft.orderFetchingEnabled || settings.every(setting =>
    !setting.required || Boolean(draft.settings[setting.key!]));
  const faviconUrl = draft.imageUrl || getTenantFaviconUrl(draft.settings.endpointUrl || undefined);
  const showIcon = !faviconUrl || imgError;

  // When background data refreshes, only update the draft if the user isn't actively editing
  if (tenant !== prevTenant) {
    setPrevTenant(tenant);
    if (!isUserEditing) {
      setDraft(getInitialDraft(tenant));
    }
  }

  const isDirty =
    draft.name !== (tenant.name || '') ||
    draft.orderProvider !== tenant.orderProvider ||
    settings.some(setting => {
      const key = setting.key!;
      return setting.inputType === 'password'
        ? draft.clearedSecretKeys.includes(key) || draft.settings[key] !== undefined
        : draft.settings[key] !== (tenant.orderProviderSettings?.[key] || '');
    }) ||
    draft.imageUrl !== (tenant.imageUrl || '') ||
    draft.orderFetchingEnabled !== (tenant.orderFetchingEnabled ?? false);

  const handleSave = () => {
    const payload: UpdateTenantRequestDto = {};
    if (draft.name !== tenant.name) payload.name = draft.name;
    const providerChanged = draft.orderProvider !== tenant.orderProvider;
    if (providerChanged) payload.orderProvider = draft.orderProvider;
    const settingUpdates: Record<string, string | null> = {};
    for (const setting of settings) {
      const key = setting.key!;
      const value = draft.settings[key];
      if (providerChanged) {
        settingUpdates[key] = value || null;
      } else if (setting.inputType === 'password') {
        if (draft.clearedSecretKeys.includes(key)) settingUpdates[key] = null;
        else if (value !== undefined) settingUpdates[key] = value;
      } else if (value !== (tenant.orderProviderSettings?.[key] || '')) {
        settingUpdates[key] = value || null;
      }
    }
    if (Object.keys(settingUpdates).length > 0) payload.orderProviderSettings = settingUpdates;
    if (draft.imageUrl !== (tenant.imageUrl || '')) payload.imageUrl = draft.imageUrl;
    
    if (draft.orderFetchingEnabled !== (tenant.orderFetchingEnabled ?? false)) payload.orderFetchingEnabled = draft.orderFetchingEnabled;

    updateTenant.mutate(
      { id: tenant.id, payload },
      {
        onSuccess: () => {
          setIsUserEditing(false);
          setDraft(getInitialDraft(tenant));
          setTimeout(() => {
            updateTenant.reset();
          }, 3000);
        }
      }
    );
  };

  const handleCancel = () => {
    setIsUserEditing(false);
    setDraft(getInitialDraft(tenant));
  };

  const handleDelete = () => {
    if (confirm('Are you sure you want to delete this tenant?')) {
      deleteTenant.mutate(tenant.id, {
        onSuccess: () => {
          void navigate({ to: '/settings/tenants' });
        }
      });
    }
  };

  // Helper to update draft and mark as user editing
  const updateDraft = (updates: Partial<typeof draft>) => {
    setIsUserEditing(true);
    setDraft(prev => ({ ...prev, ...updates }));
  };

  return (
    <>
        <SettingsPanelHeader
          title="Edit Tenant"
          subtitle={`Editing details for ${tenant.name}`}
          iconContainerClassName={!showIcon ? 'overflow-hidden bg-transparent' : undefined}
          icon={showIcon ? <Building2 size={24} /> : <img src={faviconUrl!} alt={tenant.name || ''} className="h-full w-full object-cover" onError={() => setImgError(true)} />}
          onBack={onBack}
        >
            <div className="flex items-center gap-2">
              <Button
                onClick={handleDelete}
                disabled={!isAdmin || deleteTenant.isPending}
                variant="text"
                color="error"
                icon={<Trash2 size={16} />}
              >
                Delete Tenant
              </Button>
              <Button
                onClick={handleCancel}
                disabled={!isAdmin || !isDirty || !providerSettingsComplete || updateTenant.isPending}
                variant="text"
                color="surface"
                icon={<X size={16} />}
              >
                Cancel
              </Button>
              <Button
                onClick={handleSave}
                disabled={!isAdmin || !isDirty || updateTenant.isPending}
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
            <h3 className="text-lg font-bold text-on-surface">Tenant Details</h3>
            <FormField
              id="tenant-name"
              label="Name"
              value={draft.name}
              onChange={(e) => updateDraft({ name: e.target.value })}
              disabled={!isAdmin}
            />

            <FormField
              as="select"
              id="tenant-provider"
              label="Order Provider"
              value={draft.orderProvider}
              onChange={e => updateDraft({ orderProvider: e.target.value, settings: {}, clearedSecretKeys: [] })}
              disabled={!isAdmin}
            >
              {providers.map(provider => provider.id && <option key={provider.id} value={provider.id}>{provider.displayName || provider.id}</option>)}
            </FormField>

            {settings.map(setting => {
              const key = setting.key!;
              const isSecret = setting.inputType === 'password';
              const configured = tenant.orderProviderConfiguredSecretKeys.includes(key);
              const cleared = draft.clearedSecretKeys.includes(key);
              return (
                <FormField
                  key={key}
                  id={`tenant-provider-${key}`}
                  label={setting.label || key}
                  type={setting.inputType || 'text'}
                  value={draft.settings[key] || ''}
                  disabled={!isAdmin || cleared}
                  required={setting.required}
                  placeholder={isSecret ? (cleared ? 'Cleared' : configured ? '•••••••••••• (Type to change)' : 'Type to set') : setting.placeholder || undefined}
                  onChange={e => updateDraft({ settings: { ...draft.settings, [key]: e.target.value }, clearedSecretKeys: draft.clearedSecretKeys.filter(item => item !== key) })}
                  meta={isSecret ? (
                    <span className="flex items-center gap-3">
                      {configured && !cleared && <button type="button" onClick={() => updateDraft({ settings: { ...draft.settings, [key]: null }, clearedSecretKeys: [...draft.clearedSecretKeys, key] })} disabled={!isAdmin} className="cursor-pointer rounded-full px-3 py-1 text-sm font-bold text-error transition-colors hover:bg-error-container">Clear {setting.label || key}</button>}
                      <span className="text-xs italic text-on-surface-variant">{cleared ? 'Pending clear' : configured ? 'Configured' : 'Not configured'}</span>
                    </span>
                  ) : undefined}
                />
              );
            })}

            <FormField
              id="tenant-image-url"
              label="Image URL"
              type="url"
              value={draft.imageUrl}
              onChange={(e) => updateDraft({ imageUrl: e.target.value })}
              disabled={!isAdmin}
            />

            <CheckboxField
              id="tenant-fetching"
              label="Enable Order Fetching"
              checked={draft.orderFetchingEnabled}
              disabled={!isAdmin}
              onChange={e => updateDraft({ orderFetchingEnabled: e.target.checked })}
            />
          </div>

          <TenantMonitorsPanel tenantId={tenant.id} />
        </div>
      </div>
    </>
  );
}

export function TenantDetailView() {
  const navigate = useNavigate();
  const { tenantId } = useParams({ strict: false }) as { tenantId: string };
  const { tenants, isAdmin } = useTenantsViewModel();
  const { data: providers = [] } = useOrderProviderDescriptorsQuery();
  
  const tenant = tenants?.find(t => t.id === tenantId);

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      <SettingsPanel className="flex-1 max-h-none">
        {!tenant ? (
            <SettingsPanelHeader
                title="Edit Tenant"
                subtitle="Loading..."
                icon={<Building2 size={24} />}
                onBack={() => void navigate({ to: '/settings/tenants' })}
            />
        ) : (
            <TenantDetailForm key={tenant.id} tenant={tenant} providers={providers} isAdmin={isAdmin} onBack={() => void navigate({ to: '/settings/tenants' })} />
        )}
      </SettingsPanel>
    </div>
  );
}
