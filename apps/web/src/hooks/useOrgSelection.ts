// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';
import { ORG_SELECTION_RESET_EVENT, readStoredOrgId, writeStoredOrgId } from '../utils/orgSelection';
import { useOrganizationsForPickerQuery } from './useMembershipQueries';

export function useOrgSelection() {
  const [selectedOrgId, setSelectedOrgIdState] = useState<string | null>(() => readStoredOrgId());
  const { data: organizations = [] } = useOrganizationsForPickerQuery();

  useEffect(() => {
    const onReset = () => {
      setSelectedOrgIdState(null);
      toast.info('Organization selection reset.');
    };
    window.addEventListener(ORG_SELECTION_RESET_EVENT, onReset);
    return () => window.removeEventListener(ORG_SELECTION_RESET_EVENT, onReset);
  }, []);

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