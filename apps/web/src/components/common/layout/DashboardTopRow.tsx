// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from "react";
import {useMediaQuery} from "../../../hooks/useMediaQuery";

export function DashboardTopRow({ children }: { children: ReactNode }) {
    const isMobileView = useMediaQuery('(max-width: 767px)');

    if (isMobileView) {
        return (
            <div className="[mask-image:linear-gradient(to_right,black_85%,transparent_100%)] -mx-3">
                <div className="kpi-carousel custom-scrollbar flex flex-row overflow-x-auto snap-x snap-mandatory gap-4 p-2 scroll-pl-3">
                    {children}
                    <div className="w-3 shrink-0" />
                </div>
            </div>
        );
    }

    return (
        <section className="grid grid-cols-2 portrait-lg:grid-cols-3 landscape-lg:grid-cols-5 gap-4 shrink-0">
            {children}
        </section>
    );
}
