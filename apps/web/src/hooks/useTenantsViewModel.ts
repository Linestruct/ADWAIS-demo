// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState, useMemo } from 'react';
import { useTenantsQuery, useCreateTenantMutation, useDeleteTenantMutation } from './useTenantQueries';
import { useMonitorsQuery, useUnassignedMonitorsQuery, useCreateMonitorMutation, useDeleteMonitorMutation, useControlMonitorMutation, useAssignMonitorMutation, useUnassignMonitorMutation } from './useMonitorQueries';
import { useCurrentUser } from './useCurrentUser';
import type { UptimeMonitorDto } from '@types';
import { DEFAULT_UPTIME_MONITOR_TYPE } from '../utils/monitorTypeHelper';

export function useTenantsViewModel() {
    const { role } = useCurrentUser();
    const isAdmin = role === 'Admin';

    const [isCreatingTenant, setIsCreatingTenant] = useState(false);
    const [newTenantDraft, setNewTenantDraft] = useState({ name: '', endpointUrl: '', imageUrl: '', authorization: '' });

    const [isCreatingMonitor, setIsCreatingMonitor] = useState(false);
    const [newMonitorDraft, setNewMonitorDraft] = useState<{ name: string; url: string; type: string; uptimeSla: number | '' }>({
        name: '',
        url: '',
        type: DEFAULT_UPTIME_MONITOR_TYPE,
        uptimeSla: ''
    });

    const [tenantSort, setTenantSort] = useState<'asc' | 'desc'>('asc');
    const [tenantSearch, setTenantSearch] = useState('');
    const [tenantFilters, setTenantFilters] = useState({ token: 'all', fetch: 'all' });

    const [monitorSort, setMonitorSort] = useState<'asc' | 'desc'>('asc');
    const [monitorSearch, setMonitorSearch] = useState('');
    const [monitorFilters, setMonitorFilters] = useState<{ assignment: 'all' | 'assigned' | 'unassigned'; tag: string; status: 'all' | 'enabled' | 'disabled'; type: string }>({ assignment: 'all', tag: 'all', status: 'all', type: 'all' });

    // Queries & Mutations from custom hooks
    const tenantsQuery = useTenantsQuery();
    const monitorsQuery = useMonitorsQuery();
    const unassignedMonitorsQuery = useUnassignedMonitorsQuery();
    const { data: tenants } = tenantsQuery;
    const { data: monitors } = monitorsQuery;
    const { data: unassignedMonitors } = unassignedMonitorsQuery;

    const createTenant = useCreateTenantMutation(() => {
        setIsCreatingTenant(false);
        setNewTenantDraft({ name: '', endpointUrl: '', imageUrl: '', authorization: '' });
    });
    const deleteTenant = useDeleteTenantMutation();

    const createMonitor = useCreateMonitorMutation(() => {
        setIsCreatingMonitor(false);
        setNewMonitorDraft({ name: '', url: '', type: DEFAULT_UPTIME_MONITOR_TYPE, uptimeSla: '' });
    });
    const deleteMonitor = useDeleteMonitorMutation();
    const toggleMonitor = useControlMonitorMutation();
    const assignMonitor = useAssignMonitorMutation();
    const unassignMonitor = useUnassignMonitorMutation();

    const allMonitors = useMemo(() => {
        const list = [...(monitors || []), ...(unassignedMonitors || [])];
        const unique: UptimeMonitorDto[] = [];
        list.forEach((m) => {
            if (!unique.find((um) => um.id === m.id)) {
                unique.push(m);
            }
        });
        return unique;
    }, [monitors, unassignedMonitors]);

    const allUniqueTags = useMemo(() => {
        const tagsSet = new Set<string>();
        allMonitors.forEach((m) => {
            if (m.tags && Array.isArray(m.tags)) {
                m.tags.forEach((t) => {
                    const trimmed = t.trim().toUpperCase();
                    if (trimmed) {
                        tagsSet.add(trimmed);
                    }
                });
            }
        });
        return Array.from(tagsSet).sort();
    }, [allMonitors]);

    const allUniqueTypes = useMemo(() => {
        const typesSet = new Set<string>();
        allMonitors.forEach((m) => {
            if (m.type) typesSet.add(m.type);
        });
        return Array.from(typesSet).sort();
    }, [allMonitors]);

    const SYSTEM_TENANT_ID = '00000000-0000-0000-0000-000000000001';

    // Filter & Sort Tenants
    const sortedTenants = useMemo(() => {
        return [...(tenants || [])].filter((t) => {
            if (t.id === SYSTEM_TENANT_ID) return false;
            if (tenantSearch) {
                const q = tenantSearch.toLowerCase();
                const matchName = t.name?.toLowerCase().includes(q);
                const matchUrl = t.orderProviderSettings?.endpointUrl?.toLowerCase().includes(q);
                if (!matchName && !matchUrl) return false;
            }
            const hasToken = t.hasOrderProviderSettings;
            if (tenantFilters.token === 'set') if (!hasToken) return false;
            if (tenantFilters.token === 'missing') if (hasToken) return false;
            if (tenantFilters.fetch === 'on') if (!t.orderFetchingEnabled) return false;
            if (tenantFilters.fetch === 'off') if (t.orderFetchingEnabled) return false;
            return true;
        }).sort((a, b) => {
            return tenantSort === 'asc' ? a.name.localeCompare(b.name) : b.name.localeCompare(a.name);
        });
    }, [tenants, tenantSearch, tenantFilters, tenantSort]);

    // Filter & Sort Monitors
    const filteredAndSortedMonitors = useMemo(() => {
        return allMonitors.filter((m) => {
            if (monitorSearch) {
                const q = monitorSearch.toLowerCase();
                const matchName = (m.name || '').toLowerCase().includes(q);
                const matchUrl = m.url?.toLowerCase().includes(q);
                const matchType = m.type?.toLowerCase().includes(q);
                const matchTenant = m.tenantName?.toLowerCase().includes(q);
                if (!matchName && !matchUrl && !matchType && !matchTenant) return false;
            }
            if (monitorFilters.assignment === 'assigned' && (m.tenantId == null || m.tenantId === SYSTEM_TENANT_ID)) return false;
            if (monitorFilters.assignment === 'unassigned' && (m.tenantId != null && m.tenantId !== SYSTEM_TENANT_ID)) return false;
            if (monitorFilters.status === 'enabled' && !m.uptimeMonitorEnabled) return false;
            if (monitorFilters.status === 'disabled' && m.uptimeMonitorEnabled) return false;
            if (monitorFilters.type !== 'all' && m.type !== monitorFilters.type) return false;
            return true;
        }).filter((m) => {
            if (monitorFilters.tag === 'all') return true;
            return m.tags && m.tags.map(t => t.trim().toUpperCase()).includes(monitorFilters.tag);
        }).sort((a, b) => {
            const nameA = a.name || '';
            const nameB = b.name || '';
            return monitorSort === 'asc' ? nameA.localeCompare(nameB) : nameB.localeCompare(nameA);
        });
    }, [allMonitors, monitorSearch, monitorFilters, monitorSort]);

    return {
        isAdmin,
        tenants,
        monitors,
        unassignedMonitors,
        isTenantsLoading: tenantsQuery.isLoading,
        isTenantsError: tenantsQuery.isError,
        isMonitorsLoading: monitorsQuery.isLoading || unassignedMonitorsQuery.isLoading,
        isMonitorsError: monitorsQuery.isError || unassignedMonitorsQuery.isError,
        allMonitors,
        allUniqueTags,
        allUniqueTypes,
        sortedTenants,
        filteredAndSortedMonitors,
        isCreatingTenant,
        setIsCreatingTenant,
        newTenantDraft,
        setNewTenantDraft,
        isCreatingMonitor,
        setIsCreatingMonitor,
        newMonitorDraft,
        setNewMonitorDraft,
        tenantSort,
        setTenantSort,
        tenantSearch,
        setTenantSearch,
        tenantFilters,
        setTenantFilters,
        monitorSort,
        setMonitorSort,
        monitorSearch,
        setMonitorSearch,
        monitorFilters,
        setMonitorFilters,
        createTenant,
        deleteTenant,
        createMonitor,
        deleteMonitor,
        toggleMonitor,
        assignMonitor,
        unassignMonitor,
    };
}
