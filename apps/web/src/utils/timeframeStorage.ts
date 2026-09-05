// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { timeframeSchema, type Timeframe } from '../schemas';

export type PersistentDomain = '/financial' | '/fleet-status';

export const getSavedTimeframe = (domain: PersistentDomain): Timeframe => {
    const raw = sessionStorage.getItem(`tf_${domain}`);
    const parsed = timeframeSchema.safeParse(raw);
    return parsed.success ? parsed.data : 'T30';
};

export const setSavedTimeframe = (domain: PersistentDomain, tf: Timeframe) => {
    sessionStorage.setItem(`tf_${domain}`, tf);
};
