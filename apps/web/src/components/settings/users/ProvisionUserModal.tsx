// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { UserPlus, X } from 'lucide-react';
import { createPortal } from 'react-dom';
import { Button } from '../../common/ui/Button';
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

  return createPortal((
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-2 backdrop-blur-sm animate-in fade-in sm:p-4"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <form
        onSubmit={handleSubmit}
        className="m3-elevation-4 flex max-h-[calc(100dvh-1rem)] w-full max-w-md flex-col overflow-hidden rounded-3xl border-0 bg-surface animate-in zoom-in-95 sm:max-h-[90vh]"
        role="dialog"
        aria-modal="true"
        aria-labelledby="provision-user-title"
      >
        <div className="flex shrink-0 items-center justify-between bg-surface px-4 py-3 sm:px-6 sm:py-5">
          <h3 id="provision-user-title" className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <UserPlus size={20} className="text-on-surface-variant" aria-hidden="true" />
            Provision a user
          </h3>
          <Button
            type="button"
            onClick={onClose}
            aria-label="Close dialog"
            variant="text"
            color="surface"
            icon={<X size={20} aria-hidden="true" />}
          />
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto bg-surface px-4 pb-4 text-sm sm:px-6 sm:pb-6 sm:text-base custom-scrollbar [overflow-wrap:anywhere]">
          <div className="flex flex-col gap-4">
            <p className="mb-2 text-sm font-medium text-on-surface-variant">
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
        </div>

        <div className="flex shrink-0 justify-end gap-3 bg-surface px-4 py-3 sm:px-6 sm:py-4">
          <Button type="button" onClick={onClose} disabled={createUser.isPending} variant="text" color="surface">
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={!newUser.email || (showOrgSelect && !newUser.organizationId) || createUser.isPending}
            variant="filled"
            color="secondary"
          >
            {createUser.isPending ? 'Adding...' : 'Add user'}
          </Button>
        </div>
      </form>
    </div>
  ), document.body);
}
