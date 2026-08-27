// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useState } from 'react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { User as UserIcon, Trash2, Save, X } from 'lucide-react';
import { useUsersQuery, useUpdateUserMutation, useDeleteUserMutation } from '../../hooks/useUserQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { FormField } from '../../components/common/ui/FormField';
import { Button } from '../../components/common/ui/Button';
import { MembershipsPanel } from '../../components/settings/users/MembershipsPanel';
import { getTagColor, getTagStyle } from '../../utils/tagHelper';
import type { UserRole, UserResponseDto } from '@types';

function UserDetailForm({ user, isAdmin }: { user: UserResponseDto, isAdmin: boolean }) {
  const navigate = useNavigate();
  const updateUser = useUpdateUserMutation();
  const deleteUser = useDeleteUserMutation();

  const [draft, setDraft] = useState({
    name: user.name || '',
    email: user.email || '',
    role: (user.role as UserRole) || 'Viewer',
  });

  const isDirty = draft.name !== (user.name || '') || draft.role !== ((user.role as UserRole) || 'Viewer');

  const handleSave = () => {
    updateUser.mutate(
      { id: user.id, payload: { name: draft.name, role: draft.role } },
      {
        onSuccess: () => {
          setTimeout(() => {
            updateUser.reset();
          }, 3000);
        },
      }
    );
  };

  const handleCancel = () => {
    setDraft({
      name: user.name || '',
      email: user.email || '',
      role: (user.role as UserRole) || 'Viewer',
    });
  };

  const handleDelete = () => {
    if (confirm('Are you sure you want to delete this user?')) {
      deleteUser.mutate(user.id, {
        onSuccess: () => {
          void navigate({ to: '/settings/users' });
        },
      });
    }
  };

  return (
    <>
      <SettingsPanelHeader
        title="Edit User"
        subtitle={`Editing details for ${user.email}`}
        icon={<span className="text-lg font-bold">{user.name?.charAt(0).toUpperCase() || '?'}</span>}
        iconContainerClassName={`border ${getTagStyle(getTagColor(user.name || user.email || 'Unknown'))}`}
        onBack={() => void navigate({ to: '/settings/users' })}
      >
            <div className="flex items-center gap-2">
              <Button
                onClick={handleDelete}
                disabled={!isAdmin || deleteUser.isPending}
                variant="tonal"
                color="error"
                icon={<Trash2 size={16} />}
              >
                Revoke Access
              </Button>
              <Button
                onClick={handleCancel}
                disabled={!isAdmin || !isDirty || updateUser.isPending}
                variant="text"
                color="surface"
                icon={<X size={16} />}
              >
                Cancel
              </Button>
              <Button
                onClick={handleSave}
                disabled={!isAdmin || !isDirty || updateUser.isPending}
                variant="tonal"
                color="primary"
                icon={<Save size={16} />}
              >
                Save Changes
              </Button>
            </div>
        </SettingsPanelHeader>

      <div className="custom-scrollbar flex-1 overflow-y-auto px-6 py-6">
        <div className="mx-auto max-w-2xl space-y-6">
          <FormField
            id="user-email"
            label="Email Address"
            value={draft.email}
            disabled
            helperText="Email addresses cannot be changed."
          />

          <FormField
            id="user-name"
            label="Full Name"
            value={draft.name}
            onChange={(e) => setDraft({ ...draft, name: e.target.value })}
            disabled={!isAdmin}
          />

          <FormField
            as="select"
            id="user-role"
            label="Role"
            value={draft.role}
            onChange={(e) => setDraft({ ...draft, role: e.target.value as UserRole })}
            disabled={!isAdmin}
          >
            <option value="Admin">Admin</option>
            <option value="Viewer">Viewer</option>
            <option value="Employee">Employee</option>
          </FormField>
        </div>
      </div>
    </>
  );
}

export function UserDetailView() {
  const navigate = useNavigate();
  const { userId } = useParams({ strict: false }) as { userId: string };
  const { data: users, isLoading } = useUsersQuery();
  const { role: currentUserRole, scope } = useCurrentUser();
  const isAdmin = currentUserRole === 'Admin';

  const user = users?.find(u => u.id === userId);

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      <SettingsPanel className="flex-1 max-h-none">
        {isLoading && (
          <SettingsPanelHeader
            title="Edit User"
            subtitle="Loading..."
            icon={<UserIcon size={24} />}
            onBack={() => void navigate({ to: '/settings/users' })}
          />
        )}
        {!isLoading && !user && <div className="p-8 text-center text-error">User not found.</div>}
        {!isLoading && user && (
          <>
            <UserDetailForm key={user.id} user={user} isAdmin={isAdmin} />
            <div className="mx-auto w-full max-w-2xl px-6 pb-6">
              <MembershipsPanel userId={user.id} isAdmin={isAdmin} isPlatformAdmin={scope.isPlatformAdmin} />
            </div>
          </>
        )}
      </SettingsPanel>
    </div>
  );
}
