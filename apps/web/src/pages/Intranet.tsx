// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { BulletinBoard } from '../components/Intranet/BulletinBoard';
import { Calendar } from '../components/Intranet/Calendar';
import { ExternalNewsFeed } from '../components/Intranet/ExternalNewsFeed';
import { OrganizationNews } from '../components/Intranet/OrganizationNews';
import { TodayOverview } from '../components/Intranet/TodayOverview';
import { DashboardLayout } from '../components/common/layout/DashboardLayout';

export function Intranet() {
  return (
    <DashboardLayout>
      <div className="grid grid-cols-1 portrait-lg:grid-cols-2 portrait-contained:grid-rows-3 landscape-lg:grid-cols-3 landscape-contained:grid-rows-2 gap-4 flex-1 min-h-0 animate-in fade-in duration-500">
        {/* Top row */}
        <div className="col-span-1 md:h-[500px] contained:h-full contained:min-h-0 flex flex-col [&>*]:flex-1 [&>*]:min-h-0">
          <TodayOverview />
        </div>
        
        <div className="col-span-1 md:h-[500px] contained:h-full contained:min-h-0 flex flex-col [&>*]:flex-1 [&>*]:min-h-0">
          <BulletinBoard />
        </div>
        
        <div className="col-span-1 md:h-[500px] contained:h-full contained:min-h-0 flex flex-col [&>*]:flex-1 [&>*]:min-h-0">
          <Calendar />
        </div>
        
        {/* Bottom row */}
        <div className="col-span-1 md:h-[500px] contained:h-full contained:min-h-0 flex flex-col [&>*]:flex-1 [&>*]:min-h-0">
          <ExternalNewsFeed authorName="litium" />
        </div>
        
        <div className="col-span-1 portrait-lg:col-span-2 landscape-lg:col-span-2 md:h-[500px] contained:h-full contained:min-h-0 flex flex-col [&>*]:flex-1 [&>*]:min-h-0">
          <OrganizationNews authorName="motillo" />
        </div>
      </div>
    </DashboardLayout>
  );
}
