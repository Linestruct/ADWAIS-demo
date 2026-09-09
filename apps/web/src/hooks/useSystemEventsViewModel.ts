// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../apiClient';
import { useCurrentUser } from './useCurrentUser';
import { useOrgSelection } from './useOrgSelection';
import type { SystemHealthDto } from '@types';

export interface SystemEvent {
    id?: string | number;
    timestamp: string;
    level?: string | number | null;
    message: string;
    exception?: string;
    tenantId?: string | null;
    tenant?: {
        id: string;
        name: string;
    } | null;
    source?: string | null;
    details?: string | null;
}

export function useSystemEventsViewModel() {
    const { user } = useCurrentUser();
    const { selectedOrgId } = useOrgSelection();
    const platformScopeActive = user?.isPlatformAdmin === true && selectedOrgId === null;

    const healthQuery = useQuery<SystemHealthDto>({
        queryKey: ['system-health'],
        queryFn: () => apiFetch<SystemHealthDto>('/api/system/health'),
        enabled: platformScopeActive,
        refetchInterval: platformScopeActive ? 30000 : false
    });

    const eventsQuery = useQuery<SystemEvent[]>({
        queryKey: ['system-events'],
        queryFn: () => apiFetch<SystemEvent[]>('/api/SystemEvent?take=30'),
        refetchInterval: 30000
    });

    return {
        health: healthQuery.data,
        isLoadingHealth: healthQuery.isLoading,
        isHealthError: healthQuery.isError,
        events: eventsQuery.data,
        isLoadingEvents: eventsQuery.isLoading,
        isEventsError: eventsQuery.isError
    };
}
