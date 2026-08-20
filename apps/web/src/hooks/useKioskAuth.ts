// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { customClient } from '../apiClient';

export interface RegisterKioskRequest {
  deviceId: string;
}

export interface RegisterKioskResponse {
  activationCode: string;
}

export interface ActivateKioskRequest {
  activationCode: string;
}

export interface TokenResponse {
  token: string;
  expiresInDays: number;
}

export const kioskKeys = {
  all: ['kiosk'] as const,
  token: (deviceId: string) => [...kioskKeys.all, 'token', deviceId] as const,
};

export function useRegisterKioskMutation() {
  return useMutation<RegisterKioskResponse, Error, RegisterKioskRequest>({
    mutationFn: (payload) => customClient<RegisterKioskResponse>({
      url: '/api/kiosk/register',
      method: 'POST',
      data: payload,
    }),
  });
}

export function useActivateKioskMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  return useMutation<void, Error, ActivateKioskRequest>({
    mutationFn: (payload) => customClient<void>({
      url: '/api/kiosk/activate',
      method: 'POST',
      data: payload,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: kioskKeys.all });
      if (onSuccessCallback) onSuccessCallback();
    }
  });
}

export function useKioskTokenQuery(deviceId: string, isRegistered: boolean) {
  return useQuery<TokenResponse, Error>({
    queryKey: kioskKeys.token(deviceId),
    queryFn: () => customClient<TokenResponse>({
      url: `/api/kiosk/token?deviceId=${deviceId}`,
      method: 'GET',
      headers: { 'X-Bypass-Global-401': 'true' }
    }),
    enabled: isRegistered,
    refetchInterval: (query) => {
      if (query.state.data) return false;
      return 5000;
    },
    retry: Infinity,
    retryDelay: 5000,
  });
}
