// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Building2, Plus, Edit2, Trash2 } from 'lucide-react';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { SearchInput } from '../../components/common/ui/SearchInput';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { EmptyState } from '../../components/common/ui/EmptyState';
import { Button } from '../../components/common/ui/Button';
import { TableSkeletonRows } from '../../components/common/ui/TableSkeletonRows';
import { useTenantsViewModel } from '../../hooks/useTenantsViewModel';
import { CreateTenantModal } from '../../components/settings/tenants/CreateTenantModal';
import { TenantRow } from '../../components/settings/tenants/TenantRow';
import { TenantSettingsFilterMenu } from '../../components/settings/tenants/SettingsFilterMenu';

export function TenantsMonitorsView() {
    const navigate = useNavigate();
    const {
        isAdmin,
        tenants,
        sortedTenants,
        isTenantsLoading,
        isTenantsError,
        tenantSearch,
        setTenantSearch,
        tenantFilters,
        setTenantFilters,
        tenantSort,
        setTenantSort,
        createTenant,
        deleteTenant,
    } = useTenantsViewModel();

    const [isAddModalOpen, setIsAddModalOpen] = useState(false);
    const [selectedTenantIds, setSelectedTenantIds] = useState<Set<string>>(new Set());

    const handleSelect = (id: string) => {
        setSelectedTenantIds((prev) => {
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
        if (confirm(`Are you sure you want to delete ${selectedTenantIds.size} tenant(s)?`)) {
            const ids = Array.from(selectedTenantIds);
            ids.forEach(id => deleteTenant.mutate(id));
            setSelectedTenantIds(new Set());
        }
    };

    const handleEditSelected = () => {
        if (selectedTenantIds.size === 1) {
            const id = selectedTenantIds.values().next().value;
            if (id) {
                void navigate({ to: '/settings/tenants/$tenantId', params: { tenantId: id } });
            }
        }
    };

    return (
        <div className="flex flex-col gap-4 h-full min-h-0">
            <SettingsPanel className="flex-1 max-h-none">
                <SettingsPanelHeader
                    title="Tenants"
                    subtitle="Connected commerce environments and credentials."
                    icon={<Building2 size={24} />}
                >
                    <SearchInput
                        value={tenantSearch}
                        onChange={setTenantSearch}
                        placeholder="Search tenants..."
                    />
                    
                    <TenantSettingsFilterMenu
                        filters={tenantFilters}
                        setFilters={setTenantFilters}
                        sort={tenantSort}
                        setSort={setTenantSort}
                    />

                            <Button
                                onClick={() => setIsAddModalOpen(true)}
                                disabled={!isAdmin}
                                variant="tonal"
                                color="secondary"
                                icon={<Plus size={16} />}
                            >
                                New
                            </Button>
                            <Button
                                onClick={handleEditSelected}
                                disabled={!isAdmin || selectedTenantIds.size !== 1}
                                variant="text"
                                color="surface"
                                icon={<Edit2 size={16} />}
                            >
                                Edit
                            </Button>
                            <Button
                                onClick={handleDeleteSelected}
                                disabled={!isAdmin || selectedTenantIds.size === 0}
                                variant="text"
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
                                    <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Tenant</th>
                                    <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Litium URL</th>
                                    <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Status</th>
                                    <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Service Token</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-outline-variant" aria-busy={isTenantsLoading} aria-label={isTenantsLoading ? 'Loading tenants' : undefined}>
                                {isTenantsLoading && <TableSkeletonRows columnCount={5} />}
                                {!isTenantsLoading && !isTenantsError && sortedTenants.map((t) => (
                                    <TenantRow
                                        key={t.id}
                                        t={t}
                                        selected={selectedTenantIds.has(t.id)}
                                        onSelect={() => handleSelect(t.id)}
                                        onDoubleClick={() => void navigate({ to: '/settings/tenants/$tenantId', params: { tenantId: t.id } })}
                                    />
                                ))}
                                {!isTenantsLoading && isTenantsError && (
                                    <tr>
                                        <td colSpan={5} className="p-0">
                                            <div role="alert" className="p-8 text-center text-on-surface-variant">
                                                Unable to load tenants.
                                            </div>
                                        </td>
                                    </tr>
                                )}
                                {!isTenantsLoading && !isTenantsError && tenants?.length === 0 && (
                                    <EmptyState message="No tenants found" isTableRow colSpan={5} />
                                )}
                            </tbody>
                        </table>
                </div>
            </SettingsPanel>

            {isAdmin && (
                <CreateTenantModal 
                    isOpen={isAddModalOpen} 
                    onClose={() => setIsAddModalOpen(false)} 
                    createTenant={createTenant} 
                />
            )}
        </div>
    );
}
