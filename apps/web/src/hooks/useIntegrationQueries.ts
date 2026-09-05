// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ProviderDescriptor } from '@types';
import {
  useGetApiIntegrationsMonitoringProviders,
  useGetApiIntegrationsOrderProviders,
} from '../api/generated/endpoints';

export function useOrderProviderDescriptorsQuery() {
  return useGetApiIntegrationsOrderProviders<ProviderDescriptor[], Error>({
    query: { select: response => response.data },
  });
}

export function useMonitoringProviderDescriptorsQuery() {
  return useGetApiIntegrationsMonitoringProviders<ProviderDescriptor[], Error>({
    query: { select: response => response.data },
  });
}
