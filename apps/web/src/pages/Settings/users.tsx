// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Users, Edit2, Trash2, UserPlus } from 'lucide-react';
import { useUsersQuery, useCreateUserMutation, useDeleteUserMutation } from '../../hooks/useUserQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { UserRow } from '../../components/settings/users/UserRow';
import { ProvisionUserModal } from '../../components/settings/users/ProvisionUserModal';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { EmptyState } from '../../components/common/ui/EmptyState';
import { Button } from '../../components/common/ui/Button';
import { TableSkeletonRows } from '../../components/common/ui/TableSkeletonRows';

export function UsersView() {
    const navigate = useNavigate();
    const { data: users, isLoading, isError } = useUsersQuery();
    const createUser = useCreateUserMutation();
    const deleteUser = useDeleteUserMutation();
    const { role } = useCurrentUser();
    const isAdmin = role === 'Admin';
    const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());
    const [isAddModalOpen, setIsAddModalOpen] = useState(false);

    const handleSelect = (id: string) => {
        setSelectedUserIds((prev) => {
            const next = new Set(prev);
            if (next.has(id)) {
                next.delete(id);
            } else {
                next.add(id);
            }
            return next;
        });
    };

    const handleDeleteSelected = () => {
        if (confirm(`Are you sure you want to delete ${selectedUserIds.size} user(s)?`)) {
            const ids = Array.from(selectedUserIds);
            ids.forEach(id => deleteUser.mutate(id));
            setSelectedUserIds(new Set());
        }
    };

    const handleEditSelected = () => {
        if (selectedUserIds.size === 1) {
            const id = selectedUserIds.values().next().value;
            if (id) {
                void navigate({ to: '/settings/users/$userId', params: { userId: id } });
            }
        }
    };

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-1 portrait-contained:grid-rows-1 gap-4 h-full min-h-0">
            <div className="flex min-h-0 flex-col gap-4 h-full">
            <SettingsPanel className="flex-1 max-h-none">
                <SettingsPanelHeader
                    title="Active Users"
                    subtitle="Review access, roles and registered accounts."
                    icon={<Users size={24} />}
                >
                            <Button
                                onClick={() => setIsAddModalOpen(true)}
                                disabled={!isAdmin}
                                variant="tonal"
                                color="secondary"
                                icon={<UserPlus size={16} />}
                            >
                                Add user
                            </Button>
                            <Button
                                onClick={handleEditSelected}
                                disabled={!isAdmin || selectedUserIds.size !== 1}
                                variant="text"
                                color="surface"
                                icon={<Edit2 size={16} />}
                            >
                                Edit
                            </Button>
                            <Button
                                onClick={handleDeleteSelected}
                                disabled={!isAdmin || selectedUserIds.size === 0}
                                variant="tonal"
                                color="error"
                                icon={<Trash2 size={16} />}
                            >
                                Delete
                            </Button>
                </SettingsPanelHeader>
                <div className="border border-outline-variant bg-surface custom-scrollbar flex-1 overflow-auto">
                        <table className="w-full whitespace-nowrap text-left text-sm">
                            <thead className="sticky top-0 z-10 border-b border-outline-variant bg-surface-container-high text-on-surface-variant">
                                <tr>
                                    <th className="w-12 px-4 py-4 sm:px-5"></th>
                                    <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">User</th>
                                    <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Email</th>
                                    <th className="w-44 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Role</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-outline-variant" aria-busy={isLoading} aria-label={isLoading ? 'Loading users' : undefined}>
                                {isLoading && <TableSkeletonRows columnCount={4} />}
                                {!isLoading && !isError && (users || []).map((u) => (
                                    <UserRow
                                        key={u.id}
                                        u={u}
                                        selected={selectedUserIds.has(u.id)}
                                        onSelect={() => handleSelect(u.id)}
                                        onDoubleClick={() => void navigate({ to: '/settings/users/$userId', params: { userId: u.id } })}
                                    />
                                ))}
                                {!isLoading && isError && (
                                    <tr>
                                        <td colSpan={4} className="p-0">
                                            <div role="alert" className="p-8 text-center text-on-surface-variant">
                                                Unable to load users.
                                            </div>
                                        </td>
                                    </tr>
                                )}
                                {!isLoading && !isError && users?.length === 0 && (
                                    <EmptyState message="No active users registered." isTableRow colSpan={4} />
                                )}
                            </tbody>
                        </table>
                </div>
            </SettingsPanel>
            </div>
            
            {isAdmin && (
                <ProvisionUserModal 
                    isOpen={isAddModalOpen} 
                    onClose={() => setIsAddModalOpen(false)} 
                    createUser={createUser} 
                />
            )}
        </div>
    );
}
