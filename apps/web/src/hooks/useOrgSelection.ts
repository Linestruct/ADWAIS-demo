// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useCallback, useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ORG_SELECTION_CHECK_EVENT, readStoredOrgId, writeStoredOrgId } from '../utils/orgSelection';
import { useOrganizationsForPickerQuery } from './useMembershipQueries';

export function useOrgSelection() {
  const [selectedOrgId, setSelectedOrgIdState] = useState<string | null>(() => readStoredOrgId());
  const { data: organizations = [] } = useOrganizationsForPickerQuery();
  const queryClient = useQueryClient();

  // A denial while a selection is active refreshes the reachable list.
  useEffect(() => {
    const onDenied = () => {
      void queryClient.invalidateQueries({ queryKey: ['organizations'] });
    };
    window.addEventListener(ORG_SELECTION_CHECK_EVENT, onDenied);
    return () => window.removeEventListener(ORG_SELECTION_CHECK_EVENT, onDenied);
  }, [queryClient]);

  // Once the list settles, a selection that left it is a revocation: clear
  // it and fall back to the default scope. A loading or empty list is never
  // a reason to reset.
  useEffect(() => {
    if (
      selectedOrgId
      && organizations.length > 0
      && !organizations.some(org => org.id === selectedOrgId)
    ) {
      const timer = window.setTimeout(() => {
        writeStoredOrgId(null);
        setSelectedOrgIdState(null);
        toast.info('Organization selection reset.');
      }, 0);
      return () => window.clearTimeout(timer);
    }
  }, [organizations, selectedOrgId]);

  const setSelectedOrgId = useCallback((orgId: string | null) => {
    writeStoredOrgId(orgId);
    setSelectedOrgIdState(orgId);
  }, []);

  return {
    selectedOrgId,
    setSelectedOrgId,
    organizations,
  };
}