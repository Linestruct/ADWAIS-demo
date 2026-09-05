// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from "react";

export function DashboardLayout({ children, className = '' }: { children: ReactNode, className?: string }) {
    return (
        <div className={`flex flex-col gap-4 w-full pb-3 ${className || 'contained:min-h-full contained:flex-1 contained:min-h-0'}`}>
            {children}
        </div>
    );
}