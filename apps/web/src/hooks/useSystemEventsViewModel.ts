// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../apiClient';
import { useCurrentUser } from './useCurrentUser';
import { isAdminRole } from '../utils/roles';
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
    const queryClient = useQueryClient();
    const { role } = useCurrentUser();
    const isAdmin = isAdminRole(role);

    const healthQuery = useQuery<SystemHealthDto>({
        queryKey: ['system-health'],
        queryFn: () => apiFetch<SystemHealthDto>('/api/system/health'),
        refetchInterval: 30000
    });

    const eventsQuery = useQuery<SystemEvent[]>({
        queryKey: ['system-events'],
        queryFn: () => apiFetch<SystemEvent[]>('/api/SystemEvent?take=30'),
        refetchInterval: 30000
    });

    const clearErrorsMutation = useMutation({
        mutationFn: () => apiFetch('/api/system/health/clear-errors', { method: 'POST' }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['system-health'] });
        }
    });

    return {
        isAdmin,
        health: healthQuery.data,
        isLoadingHealth: healthQuery.isLoading,
        isHealthError: healthQuery.isError,
        events: eventsQuery.data,
        isLoadingEvents: eventsQuery.isLoading,
        isEventsError: eventsQuery.isError,
        clearErrorsMutation
    };
}
