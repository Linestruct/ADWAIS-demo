// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useSyncExternalStore } from 'react';
import { getSelectedOrgId, setSelectedOrgId, subscribeOrgSelection } from '../utils/orgSelection';
import { useOrganizationsForPickerQuery } from './useMembershipQueries';

export function useOrgSelection() {
  const selectedOrgId = useSyncExternalStore(subscribeOrgSelection, getSelectedOrgId);
  const { data: organizations = [] } = useOrganizationsForPickerQuery();

  return {
    selectedOrgId,
    setSelectedOrgId,
    organizations,
  };
}