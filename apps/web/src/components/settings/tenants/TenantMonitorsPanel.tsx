// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React, { useState, useMemo } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Link as LinkIcon, Unlink } from 'lucide-react';
import { useTenantsViewModel } from '../../../hooks/useTenantsViewModel';

import { SearchInput } from '../../common/ui/SearchInput';
import { MonitorSettingsFilterMenu } from './SettingsFilterMenu';
import { MonitorRow } from './MonitorRow';
import { Button } from '../../common/ui/Button';

interface TenantMonitorsPanelProps {
    tenantId: string;
}

export const TenantMonitorsPanel = React.memo(function TenantMonitorsPanel({
    tenantId
}: TenantMonitorsPanelProps) {
    const navigate = useNavigate();
    const { allMonitors, allUniqueTypes, allUniqueTags, assignMonitor, unassignMonitor, isAdmin } = useTenantsViewModel();
    const isAssignPending = assignMonitor.isPending;
    const isUnassignPending = unassignMonitor.isPending;
    const SYSTEM_TENANT_ID = '00000000-0000-0000-0000-000000000001';

    const [assignedSelected, setAssignedSelected] = useState<Set<number>>(new Set());
    const [availableSelected, setAvailableSelected] = useState<Set<number>>(new Set());
    
    // Filters for available monitors
    const [availableSearch, setAvailableSearch] = useState('');
    const [monitorFilters, setMonitorFilters] = useState<{ assignment: 'all' | 'assigned' | 'unassigned'; tag: string; status: 'all' | 'enabled' | 'disabled'; type: string }>({
        assignment: 'all',
        tag: 'all',
        status: 'all',
        type: 'all'
    });
    const [monitorSort, setMonitorSort] = useState<'asc' | 'desc'>('asc');

    const assignedMonitors = useMemo(() => {
        return allMonitors.filter(m => m.tenantId === tenantId).sort((a, b) => (a.name || '').localeCompare(b.name || ''));
    }, [allMonitors, tenantId]);

    const availableMonitors = useMemo(() => {
        const filtered = allMonitors.filter(m => {
            if (m.tenantId === tenantId) return false;
            
            if (availableSearch) {
                const q = availableSearch.toLowerCase();
                const matchName = (m.name || '').toLowerCase().includes(q);
                const matchUrl = m.url?.toLowerCase().includes(q);
                if (!matchName && !matchUrl) return false;
            }
            
            if (monitorFilters.assignment === 'assigned' && (m.tenantId == null || m.tenantId === SYSTEM_TENANT_ID)) return false;
            if (monitorFilters.assignment === 'unassigned' && (m.tenantId != null && m.tenantId !== SYSTEM_TENANT_ID)) return false;

            if (monitorFilters.status === 'enabled' && !m.uptimeMonitorEnabled) return false;
            if (monitorFilters.status === 'disabled' && m.uptimeMonitorEnabled) return false;

            if (monitorFilters.type !== 'all' && m.type !== monitorFilters.type) return false;

            if (monitorFilters.tag !== 'all') {
                if (!m.tags || !m.tags.includes(monitorFilters.tag)) return false;
            }
            
            return true;
        });

        filtered.sort((a, b) => {
            const nameA = a.name || '';
            const nameB = b.name || '';
            return monitorSort === 'asc' ? nameA.localeCompare(nameB) : nameB.localeCompare(nameA);
        });

        return filtered;
    }, [allMonitors, tenantId, availableSearch, monitorFilters, monitorSort]);

    const handleAssignSelected = () => {
        if (!isAdmin) return;
        const ids = Array.from(availableSelected);
        ids.forEach(id => assignMonitor.mutate({ id, tenantId }));
        setAvailableSelected(new Set());
    };

    const handleUnassignSelected = () => {
        if (!isAdmin) return;
        const ids = Array.from(assignedSelected);
        ids.forEach(id => unassignMonitor.mutate(id));
        setAssignedSelected(new Set());
    };

    const handleAssignedSelect = (id: number) => {
        setAssignedSelected((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const handleAvailableSelect = (id: number) => {
        setAvailableSelected((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const renderThead = () => (
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
    );

    return (
        <>
            {/* Assigned Monitors */}
            <div className="space-y-4">
                <div className="flex items-center justify-between pb-2">
                    <h3 className="text-lg font-bold text-on-surface">Assigned Monitors</h3>
                    <Button
                        onClick={handleUnassignSelected}
                        disabled={!isAdmin || isUnassignPending || assignedSelected.size === 0}
                        variant="text"
                        color="error"
                        icon={<Unlink size={16} />}
                        className="!min-h-9 px-4 text-sm"
                    >
                        Unassign Selected ({assignedSelected.size})
                    </Button>
                </div>

                <div className="border border-outline-variant bg-surface-container-low rounded-xl overflow-hidden bg-surface">
                    <div className="custom-scrollbar overflow-x-auto h-[500px] overflow-y-auto">
                        <table className="w-full whitespace-nowrap text-left text-sm">
                            {renderThead()}
                            <tbody className="">
                                {assignedMonitors.map(m => (
                                    <MonitorRow
                                        key={m.id}
                                        m={m}
                                        selected={m.id !== undefined ? assignedSelected.has(m.id) : false}
                                        onSelect={() => m.id !== undefined && handleAssignedSelect(m.id)}
                                        onDoubleClick={() => m.id !== undefined && void navigate({ to: '/settings/monitors/$monitorId', params: { monitorId: String(m.id) } })}
                                    />
                                ))}
                                {assignedMonitors.length === 0 && (
                                    <tr>
                                        <td colSpan={6} className="px-4 py-8 text-center text-on-surface-variant italic sm:px-5">
                                            No monitors are currently assigned to this tenant.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            {/* Available Monitors */}
            <div className="space-y-4 xl:col-span-2">
                <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 pb-4">
                    <div className="flex-1 space-y-1">
                        <h3 className="text-lg font-bold text-on-surface">Assignable Monitors</h3>
                        <p className="text-base text-on-surface-variant">Select monitors to assign to this tenant. If a monitor is already assigned to another tenant, it will be reassigned.</p>
                    </div>
                    
                    <Button
                        onClick={handleAssignSelected}
                        disabled={!isAdmin || isAssignPending || availableSelected.size === 0}
                        variant="tonal"
                        color="primary"
                        icon={<LinkIcon size={16} />}
                        className="shrink-0"
                    >
                        Assign Selected ({availableSelected.size})
                    </Button>
                </div>

                <div className="flex flex-wrap items-center gap-3 pb-2">
                    <SearchInput
                        value={availableSearch}
                        onChange={setAvailableSearch}
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
                </div>

                    <div className="border border-outline-variant rounded-xl overflow-hidden bg-surface">
                        <div className="custom-scrollbar overflow-x-auto h-[500px] overflow-y-auto">
                            <table className="w-full whitespace-nowrap text-left text-sm">
                                {renderThead()}
                                <tbody className="divide-y divide-outline-variant">
                                    {availableMonitors.map(m => (
                                        <MonitorRow
                                            key={m.id}
                                            m={m}
                                            selected={m.id !== undefined ? availableSelected.has(m.id) : false}
                                            onSelect={() => m.id !== undefined && handleAvailableSelect(m.id)}
                                            onDoubleClick={() => m.id !== undefined && void navigate({ to: '/settings/monitors/$monitorId', params: { monitorId: String(m.id) } })}
                                        />
                                    ))}
                                    {availableMonitors.length === 0 && (
                                        <tr>
                                            <td colSpan={6} className="px-4 py-8 text-center text-on-surface-variant italic sm:px-5">
                                                No monitors available to assign.
                                            </td>
                                        </tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
        </>
    );
});
