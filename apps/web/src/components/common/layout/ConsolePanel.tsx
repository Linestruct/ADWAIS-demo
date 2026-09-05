// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';

interface ConsolePanelProps {
    icon: ReactNode;
    title: string;
    subtitle: string;
    children: ReactNode;
    className?: string;
}

export function ConsolePanel({ icon, title, subtitle, children, className = '' }: ConsolePanelProps) {
    return (
        <section className={`flex h-full min-h-0 min-w-0 max-w-full flex-col overflow-hidden rounded-xl border break-all border-console-border bg-console-bg ${className}`}>
            <div className="z-10 flex shrink-0 items-center justify-between border-b border-console-border bg-console-header px-3 py-4 min-w-0">
                <div className="flex items-center gap-3 min-w-0">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-console-hover-dark text-console-icon">
                        {icon}
                    </div>
                    <div className="min-w-0 flex flex-col">
                        <h2 className="m-0 text-base font-black uppercase tracking-wide text-on-surface-variant sm:text-lg">{title}</h2>
                        <p className="m-0 text-sm font-bold leading-5 text-on-surface-variant">{subtitle}</p>
                    </div>
                </div>
            </div>
            <div className="custom-scrollbar flex flex-1 flex-col overflow-y-auto overflow-x-hidden bg-console-bg font-mono text-sm">
                {children}
            </div>
        </section>
    );
}
