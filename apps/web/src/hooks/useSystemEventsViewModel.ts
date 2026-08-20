// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../apiClient';
import { useCurrentUser } from './useCurrentUser';
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
    const isAdmin = role === 'Admin';

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
