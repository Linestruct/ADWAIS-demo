// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Building2, X } from 'lucide-react';
import type { CreateTenantRequestDto } from '@types';
import { FormField } from '../../common/ui/FormField';
import { useOrderProviderDescriptorsQuery } from '../../../hooks/useIntegrationQueries';

interface CreateTenantModalProps {
  isOpen: boolean;
  onClose: () => void;
  createTenant: {
    mutate: (tenant: CreateTenantRequestDto, options?: { onSuccess?: () => void }) => void;
    isPending: boolean;
  };
}

export function CreateTenantModal({ isOpen, onClose, createTenant }: CreateTenantModalProps) {
  const { data: providers = [] } = useOrderProviderDescriptorsQuery();
  const [draft, setDraft] = useState({ name: '', imageUrl: '', orderProvider: '', settings: {} as Record<string, string> });

  if (!isOpen) return null;

  const orderProvider = draft.orderProvider || providers[0]?.id || '';
  const selectedProvider = providers.find(provider => provider.id === orderProvider);
  const settings = selectedProvider?.settings?.filter(setting => setting.key) ?? [];
  const requiredSettingsComplete = settings.every(setting => !setting.required || Boolean(draft.settings[setting.key!]));

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createTenant.mutate({
      name: draft.name,
      imageUrl: draft.imageUrl || null,
      orderProvider,
      orderProviderSettings: draft.settings,
      type: 'B2B',
      orderFetchingEnabled: false,
    }, {
      onSuccess: () => {
        setDraft({ name: '', imageUrl: '', orderProvider: '', settings: {} });
        onClose();
      }
    });
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4 backdrop-blur-sm animate-in fade-in"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <form
        onSubmit={handleSubmit}
        className="m3-elevation-4 flex w-full max-w-md flex-col overflow-hidden rounded-3xl border-0 bg-surface animate-in zoom-in-95"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-tenant-title"
      >
        <div className="flex items-center justify-between bg-surface px-6 py-5">
          <h3 id="create-tenant-title" className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <Building2 size={20} className="text-on-surface-variant" aria-hidden="true" />
            Create tenant
          </h3>
          <button type="button" onClick={onClose} aria-label="Close dialog" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
            <X size={20} aria-hidden="true" />
          </button>
        </div>

        <div className="flex flex-col gap-4 bg-surface px-6 pb-6">
          <p className="text-sm font-medium text-on-surface-variant mb-2">
            Connect a commerce environment to the dashboard.
          </p>

          <FormField
            id="tenant-name"
            label="Name"
            type="text"
            placeholder="Tenant Name"
            value={draft.name}
            onChange={e => setDraft({ ...draft, name: e.target.value })}
            required
          />

          <FormField
            as="select"
            id="tenant-provider"
            label="Order Provider"
            value={orderProvider}
            onChange={e => setDraft({ ...draft, orderProvider: e.target.value, settings: {} })}
            required
          >
            {providers.map(provider => provider.id && (
              <option key={provider.id} value={provider.id}>{provider.displayName || provider.id}</option>
            ))}
          </FormField>

          {settings.map(setting => (
            <FormField
              key={setting.key}
              id={`tenant-provider-${setting.key}`}
              label={setting.label || setting.key!}
              type={setting.inputType || 'text'}
              placeholder={setting.placeholder || undefined}
              value={draft.settings[setting.key!] || ''}
              onChange={e => setDraft({ ...draft, settings: { ...draft.settings, [setting.key!]: e.target.value } })}
              required={setting.required}
              className={setting.inputType === 'password' ? 'font-mono' : undefined}
            />
          ))}

          <FormField
            id="tenant-image"
            label="Image URL (optional)"
            type="url"
            placeholder="https://example.com/logo.png"
            value={draft.imageUrl}
            onChange={e => setDraft({ ...draft, imageUrl: e.target.value })}
          />

        </div>

        <div className="flex justify-end gap-3 bg-surface px-6 py-4">
          <button type="button" onClick={onClose} disabled={createTenant.isPending} className="inline-flex min-h-11 items-center justify-center rounded-full px-4 text-base font-bold transition-colors hover:bg-surface-container-high focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary disabled:cursor-not-allowed disabled:text-on-surface/[0.38] disabled:hover:bg-transparent">
            Cancel
          </button>
          <button type="submit" disabled={!draft.name || !orderProvider || !requiredSettingsComplete || createTenant.isPending} className="inline-flex min-h-11 items-center justify-center rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]">
            {createTenant.isPending ? 'Creating...' : 'Create tenant'}
          </button>
        </div>
      </form>
    </div>
  );
}
