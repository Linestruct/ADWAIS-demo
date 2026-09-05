// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Activity, Plus, Edit2, Trash2 } from 'lucide-react';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { SearchInput } from '../../components/common/ui/SearchInput';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { EmptyState } from '../../components/common/ui/EmptyState';
import { Button } from '../../components/common/ui/Button';
import { TableSkeletonRows } from '../../components/common/ui/TableSkeletonRows';
import { useTenantsViewModel } from '../../hooks/useTenantsViewModel';
import { CreateMonitorModal } from '../../components/settings/tenants/CreateMonitorModal';
import { MonitorRow } from '../../components/settings/tenants/MonitorRow';
import { MonitorSettingsFilterMenu } from '../../components/settings/tenants/SettingsFilterMenu';

export function MonitorsView() {
    const navigate = useNavigate();
    const {
        isAdmin,
        allMonitors,
        filteredAndSortedMonitors,
        isMonitorsLoading,
        isMonitorsError,
        monitorSearch,
        setMonitorSearch,
        monitorFilters,
        setMonitorFilters,
        monitorSort,
        setMonitorSort,
        allUniqueTags,
        allUniqueTypes,
        createMonitor,
        deleteMonitor,
    } = useTenantsViewModel();

    const [isAddModalOpen, setIsAddModalOpen] = useState(false);
    const [selectedMonitorIds, setSelectedMonitorIds] = useState<Set<number>>(new Set());

    const handleSelect = (id: number) => {
        setSelectedMonitorIds((prev) => {
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
        if (confirm(`Are you sure you want to delete ${selectedMonitorIds.size} monitor(s)?`)) {
            const ids = Array.from(selectedMonitorIds);
            ids.forEach(id => deleteMonitor.mutate(id));
            setSelectedMonitorIds(new Set());
        }
    };

    const handleEditSelected = () => {
        if (selectedMonitorIds.size === 1) {
            const id = selectedMonitorIds.values().next().value;
            if (id !== undefined) {
                void navigate({ to: '/settings/monitors/$monitorId', params: { monitorId: String(id) } });
            }
        }
    };

    return (
        <div className="flex flex-col gap-4 h-full min-h-0">
            <SettingsPanel className="flex-1 max-h-none">
                <SettingsPanelHeader
                    title="Fleet Monitors"
                    subtitle="External availability and response-time checks."
                    icon={<Activity size={24} />}
                >
                    <SearchInput
                        value={monitorSearch}
                        onChange={setMonitorSearch}
                        placeholder="Search monitors..."
                    />

                    <MonitorSettingsFilterMenu
                        filters={monitorFilters}
                        setFilters={setMonitorFilters}
                        sort={monitorSort}
                        setSort={setMonitorSort}
                        tags={allUniqueTags}
                        types={allUniqueTypes}
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
                                disabled={!isAdmin || selectedMonitorIds.size !== 1}
                                variant="text"
                                color="surface"
                                icon={<Edit2 size={16} />}
                            >
                                Edit
                            </Button>
                            <Button
                                onClick={handleDeleteSelected}
                                disabled={!isAdmin || selectedMonitorIds.size === 0}
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
                                    <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Monitor</th>
                                    <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">URL</th>
                                    <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Type</th>
                                    <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Assignment</th>
                                    <th className="w-32 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Status</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-outline-variant" aria-busy={isMonitorsLoading} aria-label={isMonitorsLoading ? 'Loading monitors' : undefined}>
                                {isMonitorsLoading && <TableSkeletonRows columnCount={6} />}
                                {!isMonitorsLoading && !isMonitorsError && filteredAndSortedMonitors.map((m) => (
                                    <MonitorRow
                                        key={m.id}
                                        m={m}
                                        selected={m.id !== undefined ? selectedMonitorIds.has(m.id) : false}
                                        onSelect={() => m.id !== undefined && handleSelect(m.id)}
                                        onDoubleClick={() => m.id !== undefined && void navigate({ to: '/settings/monitors/$monitorId', params: { monitorId: String(m.id) } })}
                                    />
                                ))}
                                {!isMonitorsLoading && isMonitorsError && (
                                    <tr>
                                        <td colSpan={6} className="p-0">
                                            <div role="alert" className="p-8 text-center text-on-surface-variant">
                                                Unable to load monitors.
                                            </div>
                                        </td>
                                    </tr>
                                )}
                                {!isMonitorsLoading && !isMonitorsError && allMonitors.length === 0 && (
                                    <EmptyState message="No monitors found" isTableRow colSpan={6} />
                                )}
                            </tbody>
                        </table>
                </div>
            </SettingsPanel>

            {isAdmin && (
                <CreateMonitorModal 
                    isOpen={isAddModalOpen} 
                    onClose={() => setIsAddModalOpen(false)} 
                    createMonitor={createMonitor} 
                />
            )}
        </div>
    );
}
