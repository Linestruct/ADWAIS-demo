// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { createLazyFileRoute } from '@tanstack/react-router';
import { Financial } from '../pages/Financial';

export const Route = createLazyFileRoute('/financial')({
  component: Financial,
});
