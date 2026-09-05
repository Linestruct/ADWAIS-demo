// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from "react";

interface FlexRowProps {
    children: ReactNode;
    weight?: 'flex-1' | 'flex-2' | 'flex-none';
    gridCols?: '1' | '2' | '3' | '5'
    className?: string;
}

export function DashboardFlexRow({ children, weight = 'flex-1', gridCols = '1', className = '' }: FlexRowProps) {
    const gridClass = {
        '1': 'grid-cols-1',
        '2': 'grid-cols-1 portrait-lg:grid-cols-1 portrait-contained:grid-rows-4 landscape-lg:grid-cols-2',
        '3': 'grid-cols-1 md:grid-cols-2 portrait-lg:grid-cols-2 landscape-lg:grid-cols-3',
        '5': 'grid-cols-1 portrait-lg:grid-cols-1 portrait-contained:grid-rows-3 landscape-lg:grid-cols-5',
    }[gridCols];

    return (
        <section className={`${weight} grid ${gridClass} auto-rows-auto contained:auto-rows-fr gap-4 ${className} min-h-0`}>
            {children}
        </section>
    )
}
