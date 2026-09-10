// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { UserCog, Trash2, Plus } from 'lucide-react';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { FormField } from '../../common/ui/FormField';
import { Button } from '../../common/ui/Button';
import {
  useMembershipsQuery,
  useOrganizationsForPickerQuery,
  useAddMembershipMutation,
  useRemoveMembershipMutation,
} from '../../../hooks/useMembershipQueries';
import type { UserRole } from '@types';

interface MembershipsPanelProps {
  userId: string;
  isAdmin: boolean;
  isPlatformAdmin: boolean;
}

const PLATFORM_VALUE = '';

export function MembershipsPanel({ userId, isAdmin, isPlatformAdmin }: MembershipsPanelProps) {
  const { data: memberships = [] } = useMembershipsQuery(userId);
  const { data: organizations = [] } = useOrganizationsForPickerQuery();
  const addMembership = useAddMembershipMutation(userId);
  const removeMembership = useRemoveMembershipMutation(userId);

  const [organizationId, setOrganizationId] = useState('');
  const [role, setRole] = useState<UserRole>('Viewer');

  const effectiveOrganizationId = organizationId || (!isPlatformAdmin ? organizations[0]?.id ?? '' : PLATFORM_VALUE);
  const isPlatformRow = isPlatformAdmin && effectiveOrganizationId === PLATFORM_VALUE;
  const orgOptions = isPlatformAdmin
    ? [{ label: 'Platform (no organization)', value: PLATFORM_VALUE }, ...organizations.map(org => ({ label: org.name, value: org.id }))]
    : organizations.map(org => ({ label: org.name, value: org.id }));
  const roleOptions = isPlatformRow
    ? [{ label: 'Platform Admin', value: 'PlatformAdmin' as UserRole }]
    : [{ label: 'Admin', value: 'Admin' as UserRole }, { label: 'Viewer', value: 'Viewer' as UserRole }, { label: 'Employee', value: 'Employee' as UserRole }];

  const handleAdd = () => {
    addMembership.mutate({
      organizationId: isPlatformRow ? null : effectiveOrganizationId,
      role: isPlatformRow ? 'PlatformAdmin' : role,
    });
  };

  return (
    <SettingsPanel
      title="Memberships"
      subtitle="Organizations this user belongs to"
      icon={<UserCog size={24} />}
    >
      <div className="flex flex-col gap-4">
        {memberships.length === 0 ? (
          <p className="px-2 text-sm font-bold text-on-surface-variant">No memberships found.</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {memberships.map(membership => (
              <li
                key={membership.id}
                className="flex min-w-0 items-center justify-between gap-3 rounded-xl bg-surface-container-low px-4 py-3"
              >
                <div className="flex min-w-0 flex-col">
                  <span className="truncate font-bold text-on-surface">
                    {membership.organizationName ?? 'Platform'}
                  </span>
                  <span className="text-sm font-bold text-on-surface-variant">{membership.role}</span>
                </div>
                {isAdmin && (
                  <button
                    type="button"
                    aria-label={`Remove membership ${membership.id}`}
                    onClick={() => removeMembership.mutate(membership.id)}
                    disabled={removeMembership.isPending}
                    className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-error transition-colors hover:bg-error-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-error disabled:cursor-not-allowed disabled:text-on-surface/[0.38]"
                  >
                    <Trash2 size={16} />
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}

        {isAdmin && (
          <div className="flex flex-col gap-3 border-t border-outline-variant pt-4">
            <FormField
              as="select"
              id="membership-organization"
              label="Organization"
              value={effectiveOrganizationId}
              onChange={(e) => setOrganizationId(e.target.value)}
            >
              {orgOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </FormField>
            <FormField
              as="select"
              id="membership-role"
              label="Role"
              value={isPlatformRow ? 'PlatformAdmin' : role}
              onChange={(e) => setRole(e.target.value as UserRole)}
            >
              {roleOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </FormField>
            <Button
              onClick={handleAdd}
              disabled={addMembership.isPending || (!isPlatformRow && !effectiveOrganizationId)}
              variant="tonal"
              color="primary"
              icon={<Plus size={16} />}
            >
              Add Membership
            </Button>
          </div>
        )}
      </div>
    </SettingsPanel>
  );
}
