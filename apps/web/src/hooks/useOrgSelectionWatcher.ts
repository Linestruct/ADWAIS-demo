// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ORG_SELECTION_CHECK_EVENT, setSelectedOrgId } from '../utils/orgSelection';
import { useOrgSelection } from './useOrgSelection';

/**
 * Mounted once at the root. Owns the selection lifecycle side effects:
 * refreshing the reachable list after a denial, and clearing a selection
 * that left the reachable list (a revocation).
 */
export function useOrgSelectionWatcher() {
  const { selectedOrgId, organizations } = useOrgSelection();
  const queryClient = useQueryClient();

  useEffect(() => {
    const onDenied = () => {
      void queryClient.invalidateQueries({ queryKey: ['organizations'] });
    };
    window.addEventListener(ORG_SELECTION_CHECK_EVENT, onDenied);
    return () => window.removeEventListener(ORG_SELECTION_CHECK_EVENT, onDenied);
  }, [queryClient]);

  useEffect(() => {
    // A loading or empty list is never a reason to reset.
    if (
      selectedOrgId
      && organizations.length > 0
      && !organizations.some(org => org.id === selectedOrgId)
    ) {
      setSelectedOrgId(null);
      toast.info('Organization selection reset.');
    }
  }, [organizations, selectedOrgId]);
}