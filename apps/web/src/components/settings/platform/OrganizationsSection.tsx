// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import {
  useCreateOrganizationMutation,
  useDeleteOrganizationMutation,
  useOrganizationSummariesQuery,
  useRenameOrganizationMutation,
} from '../../../hooks/useOrganizationQueries';
import { useCurrentUser } from '../../../hooks/useCurrentUser';
import { Button } from '../../common/ui/Button';
import { EmptyState } from '../../common/ui/EmptyState';
import { FormField } from '../../common/ui/FormField';
import { TableSkeletonRows } from '../../common/ui/TableSkeletonRows';

export function OrganizationsSection() {
  const { user, isLoading: isUserLoading } = useCurrentUser();
  const { data: organizations, isLoading, isError } = useOrganizationSummariesQuery();

  const [isCreating, setIsCreating] = useState(false);
  const [newName, setNewName] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draftName, setDraftName] = useState('');

  const createOrganization = useCreateOrganizationMutation(() => setIsCreating(false));
  const renameOrganization = useRenameOrganizationMutation();
  const deleteOrganization = useDeleteOrganizationMutation();

  const canWrite = user?.isPlatformAdmin === true;

  const handleCreate = () => {
    const name = newName.trim();
    if (!name) return;
    createOrganization.mutate({ data: { name } });
    setNewName('');
  };

  const abortCreate = () => {
    setIsCreating(false);
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

  const abortRename = () => {
    setEditingId(null);
    setDraftName('');
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
    <section>
      <h3 className="text-lg font-bold text-on-surface">Organizations</h3>
      <p className="mt-1 text-sm text-on-surface-variant">
        Manage the organizations on this deployment.
      </p>

      <div className="mt-4">
          <Button
            onClick={() => setIsCreating((open) => !open)}
            disabled={!canWrite}
            variant="tonal"
            color="secondary"
            icon={<Plus size={16} />}
          >
            Add organization
          </Button>
      </div>

      {isCreating && (
        <div className="mt-4 flex flex-wrap items-center gap-3">
          <FormField
            id="new-organization-name"
            label="New organization name"
            hideLabel
            placeholder="Organization name"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            containerClassName="min-w-52 flex-1"
          />
          <Button
            onClick={handleCreate}
            disabled={!newName.trim() || !canWrite || createOrganization.isPending}
            variant="tonal"
            color="secondary"
          >
            Create
          </Button>
          <Button onClick={abortCreate} variant="text" color="surface">
            Cancel
          </Button>
        </div>
      )}

      <div className="mt-4 border border-outline-variant rounded-xl overflow-hidden bg-surface custom-scrollbar overflow-auto">
        <table className="w-full whitespace-nowrap text-left text-sm">
          <thead className="sticky top-0 z-10 border-b border-outline-variant bg-surface-container-high text-on-surface-variant">
            <tr>
              <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Name</th>
              <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Members</th>
              <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Monitors</th>
              <th className="w-52 px-4 py-4 sm:px-5"></th>
            </tr>
          </thead>
          <tbody
            className="divide-y divide-outline-variant"
            aria-busy={isUserLoading || isLoading}
            aria-label={isUserLoading || isLoading ? 'Loading organizations' : undefined}
          >
            {(isUserLoading || isLoading) && <TableSkeletonRows columnCount={4} />}
            {!isUserLoading &&
              !isLoading &&
              !isError &&
              (organizations || []).map((org) => (
                <tr key={org.id}>
                  <td className="px-4 py-3 sm:px-5">
                    {editingId === org.id ? (
                      <FormField
                        id={`rename-organization-${org.id}`}
                        label={`Rename ${org.name}`}
                        hideLabel
                        value={draftName}
                        onChange={(e) => setDraftName(e.target.value)}
                        containerClassName="w-full max-w-64"
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
                        <>
                          <Button
                            onClick={() => handleRename(org.id)}
                            disabled={!draftName.trim() || !canWrite || renameOrganization.isPending}
                            variant="text"
                            color="secondary"
                          >
                            Save
                          </Button>
                          <Button onClick={abortRename} variant="text" color="surface">
                            Cancel
                          </Button>
                        </>
                      ) : (
                        <Button
                          onClick={() => {
                            setEditingId(org.id);
                            setDraftName(org.name ?? '');
                          }}
                          disabled={!canWrite}
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
                        disabled={!canWrite || deleteOrganization.isPending}
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
            {!isUserLoading && !isLoading && isError && (
              <tr>
                <td colSpan={4} className="p-0">
                  <div role="alert" className="p-8 text-center text-on-surface-variant">
                    Unable to load organizations.
                  </div>
                </td>
              </tr>
            )}
            {!isUserLoading && !isLoading && !isError && organizations?.length === 0 && (
              <EmptyState message="No organizations yet." isTableRow colSpan={4} />
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
