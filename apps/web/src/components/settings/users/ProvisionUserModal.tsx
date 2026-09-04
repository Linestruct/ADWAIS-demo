// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useState } from 'react';
import { UserPlus, X } from 'lucide-react';
import { FormField } from '../../common/ui/FormField';
import { useCurrentUser } from '../../../hooks/useCurrentUser';
import { useOrganizationsForPickerQuery } from '../../../hooks/useMembershipQueries';

interface ProvisionUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  createUser: {
    mutate: (user: { email: string; role: string; organizationId?: string | null }, options?: { onSuccess?: () => void }) => void;
    isPending: boolean;
  };
}

export function ProvisionUserModal({ isOpen, onClose, createUser }: ProvisionUserModalProps) {
  const [newUser, setNewUser] = useState({ email: '', role: 'Admin', organizationId: '' });
  const { user } = useCurrentUser();
  const { data: organizations } = useOrganizationsForPickerQuery();
  const showOrgSelect = user?.isPlatformAdmin === true;

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createUser.mutate(
      {
        email: newUser.email,
        role: newUser.role,
        organizationId: showOrgSelect && newUser.organizationId ? newUser.organizationId : null,
      },
      {
        onSuccess: () => {
          setNewUser({ email: '', role: 'Admin', organizationId: '' });
          onClose();
        },
      },
    );
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
        aria-labelledby="provision-user-title"
      >
        <div className="flex items-center justify-between bg-surface px-6 py-5">
          <h3 id="provision-user-title" className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <UserPlus size={20} className="text-on-surface-variant" aria-hidden="true" />
            Provision a user
          </h3>
          <button type="button" onClick={onClose} aria-label="Close dialog" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
            <X size={20} aria-hidden="true" />
          </button>
        </div>

        <div className="flex flex-col gap-4 bg-surface px-6 pb-6">
          <p className="text-sm font-medium text-on-surface-variant mb-2">
            Add an account and choose its initial access level.
          </p>

          <FormField
            id="new-user-email"
            label="Email Address"
            type="email"
            placeholder="Email Address"
            value={newUser.email}
            onChange={e => setNewUser({ ...newUser, email: e.target.value })}
            required
          />

          <FormField
            as="select"
            id="new-user-role"
            label="Role"
            value={newUser.role}
            onChange={e => setNewUser({ ...newUser, role: e.target.value })}
          >
            <option value="Admin">Admin</option>
            <option value="Viewer">Viewer</option>
            <option value="Employee">Employee</option>
          </FormField>

          {showOrgSelect && (
            <FormField
              as="select"
              id="new-user-organization"
              label="Organization"
              value={newUser.organizationId}
              onChange={e => setNewUser({ ...newUser, organizationId: e.target.value })}
              required
            >
              <option value="">Select organization</option>
              {(organizations || []).map((org) => (
                <option key={org.id} value={org.id ?? ''}>
                  {org.name}
                </option>
              ))}
            </FormField>
          )}
        </div>

        <div className="flex justify-end gap-3 bg-surface px-6 py-4">
          <button type="button" onClick={onClose} disabled={createUser.isPending} className="inline-flex min-h-11 items-center justify-center rounded-full px-4 text-base font-bold transition-colors hover:bg-surface-container-high focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary disabled:cursor-not-allowed disabled:text-on-surface/[0.38] disabled:hover:bg-transparent">
            Cancel
          </button>
          <button type="submit" disabled={!newUser.email || (showOrgSelect && !newUser.organizationId) || createUser.isPending} className="inline-flex min-h-11 items-center justify-center rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]">
            {createUser.isPending ? 'Adding...' : 'Add user'}
          </button>
        </div>
      </form>
    </div>
  );
}
