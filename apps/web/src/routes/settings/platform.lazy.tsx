// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute } from '@tanstack/react-router';
import { PlatformConfigurationView } from '../../pages/Settings/platform';

export const Route = createLazyFileRoute('/settings/platform')({
  component: PlatformConfigurationView,
});