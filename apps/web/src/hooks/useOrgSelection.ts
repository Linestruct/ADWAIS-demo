// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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