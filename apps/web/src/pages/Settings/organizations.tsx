// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useState } from 'react';
import { Building2, Pencil, Plus, Trash2 } from 'lucide-react';
import {
  useCreateOrganizationMutation,
  useDeleteOrganizationMutation,
  useOrganizationSummariesQuery,
  useRenameOrganizationMutation,
} from '../../hooks/useOrganizationQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { EmptyState } from '../../components/common/ui/EmptyState';
import { Button } from '../../components/common/ui/Button';
import { TableSkeletonRows } from '../../components/common/ui/TableSkeletonRows';

export function OrganizationsView() {
  const { user } = useCurrentUser();
  const { data: organizations, isLoading, isError } = useOrganizationSummariesQuery();

  const [isCreating, setIsCreating] = useState(false);
  const [newName, setNewName] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draftName, setDraftName] = useState('');

  const createOrganization = useCreateOrganizationMutation(() => setIsCreating(false));
  const renameOrganization = useRenameOrganizationMutation();
  const deleteOrganization = useDeleteOrganizationMutation();

  if (user?.isPlatformAdmin !== true) {
    return (
      <div className="grid grid-cols-1 gap-4 h-full min-h-0">
        <SettingsPanel className="flex-1 max-h-none">
          <SettingsPanelHeader
            title="Organizations"
            subtitle="Manage the organizations on this deployment."
            icon={<Building2 size={24} />}
          />
          <div role="alert" className="p-8 text-center text-on-surface-variant">
            Platform administrators only.
          </div>
        </SettingsPanel>
      </div>
    );
  }

  const handleCreate = () => {
    const name = newName.trim();
    if (!name) return;
    createOrganization.mutate({ data: { name } });
    setNewName('');
  };

  const handleRename = (id: string) => {
    const name = draftName.trim();
    if (!name) return;
    renameOrganization.mutate(
      { id, data: { name } },
      { onSuccess: () => setEditingId(null) },
    );
  };

  const handleDelete = (id: string, name: string | null) => {
    if (
      confirm(
        `Permanently delete "${name ?? 'this organization'}"? This removes its tenants, monitors, history, members, and schedules. This cannot be undone.`,
      )
    ) {
      deleteOrganization.mutate({ id });
    }
  };

  return (
    <div className="grid grid-cols-1 gap-4 h-full min-h-0">
      <div className="flex min-h-0 flex-col gap-4 h-full">
        <SettingsPanel className="flex-1 max-h-none">
          <SettingsPanelHeader
            title="Organizations"
            subtitle="Manage the organizations on this deployment."
            icon={<Building2 size={24} />}
          >
            <Button
              onClick={() => setIsCreating((open) => !open)}
              variant="tonal"
              color="secondary"
              icon={<Plus size={16} />}
            >
              Add organization
            </Button>
          </SettingsPanelHeader>

          {isCreating && (
            <div className="flex flex-wrap items-center gap-3 px-6 py-4">
              <input
                aria-label="New organization name"
                className="min-h-11 min-w-52 flex-1 rounded-xl border border-outline-variant bg-surface px-4 text-base"
                placeholder="Organization name"
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
              />
              <Button
                onClick={handleCreate}
                disabled={!newName.trim() || createOrganization.isPending}
                variant="tonal"
                color="secondary"
              >
                Create
              </Button>
            </div>
          )}

          <div className="border border-outline-variant bg-surface custom-scrollbar flex-1 overflow-auto">
            <table className="w-full whitespace-nowrap text-left text-sm">
              <thead className="sticky top-0 z-10 border-b border-outline-variant bg-surface-container-high text-on-surface-variant">
                <tr>
                  <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Name</th>
                  <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Members</th>
                  <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Monitors</th>
                  <th className="w-44 px-4 py-4 sm:px-5"></th>
                </tr>
              </thead>
              <tbody
                className="divide-y divide-outline-variant"
                aria-busy={isLoading}
                aria-label={isLoading ? 'Loading organizations' : undefined}
              >
                {isLoading && <TableSkeletonRows columnCount={4} />}
                {!isLoading &&
                  !isError &&
                  (organizations || []).map((org) => (
                    <tr key={org.id}>
                      <td className="px-4 py-3 sm:px-5">
                        {editingId === org.id ? (
                          <input
                            aria-label={`Rename ${org.name}`}
                            className="min-h-10 w-full max-w-64 rounded-xl border border-outline-variant bg-surface px-3 text-sm"
                            value={draftName}
                            onChange={(e) => setDraftName(e.target.value)}
                          />
                        ) : (
                          <span className="font-bold">{org.name}</span>
                        )}
                      </td>
                      <td className="px-4 py-3 sm:px-5">{org.memberCount}</td>
                      <td className="px-4 py-3 sm:px-5">{org.monitorCount}</td>
                      <td className="px-4 py-3 sm:px-5">
                        <div className="flex items-center gap-1">
                          {editingId === org.id ? (
                            <Button
                              onClick={() => handleRename(org.id)}
                              disabled={!draftName.trim() || renameOrganization.isPending}
                              variant="text"
                              color="secondary"
                            >
                              Save
                            </Button>
                          ) : (
                            <Button
                              onClick={() => {
                                setEditingId(org.id);
                                setDraftName(org.name ?? '');
                              }}
                              variant="text"
                              color="surface"
                              icon={<Pencil size={16} />}
                              aria-label={`Rename ${org.name}`}
                            >
                              Rename
                            </Button>
                          )}
                          <Button
                            onClick={() => handleDelete(org.id, org.name)}
                            disabled={deleteOrganization.isPending}
                            variant="text"
                            color="error"
                            icon={<Trash2 size={16} />}
                            aria-label={`Delete ${org.name}`}
                          >
                            Delete
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                {!isLoading && isError && (
                  <tr>
                    <td colSpan={4} className="p-0">
                      <div role="alert" className="p-8 text-center text-on-surface-variant">
                        Unable to load organizations.
                      </div>
                    </td>
                  </tr>
                )}
                {!isLoading && !isError && organizations?.length === 0 && (
                  <EmptyState message="No organizations yet." isTableRow colSpan={4} />
                )}
              </tbody>
            </table>
          </div>
        </SettingsPanel>
      </div>
    </div>
  );
}
