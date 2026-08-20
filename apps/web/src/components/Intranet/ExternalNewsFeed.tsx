// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { CollectionPanel } from '../common/dashboard/CollectionPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { ErrorAlert } from '../common/ui/ErrorAlert';
import { useGetApiIntranetFeeds } from '../../api/generated/endpoints';
import {ExternalLink} from "lucide-react";
import { formatDateTime } from '../../utils/dateTime';

type ExternalNewsFeedProps = {
  authorName: string;
  title?: string;
};

export function ExternalNewsFeed({ authorName, title = 'Industry news' }: ExternalNewsFeedProps) {
  const { data: response, isLoading, isError } = useGetApiIntranetFeeds({ PageSize: 10, AuthorName: authorName });
  const feedItems = response?.data || [];

  return (
    <CollectionPanel title={title} className="h-full relative">
      {isLoading ? (
        <div className="flex flex-col animate-pulse">
          {[1, 2, 3].map((i) => (
            <div key={i} className="flex flex-col gap-4 p-5 border-b border-outline-variant last:border-b-0">
              <div className="h-3 w-24 bg-surface-container-high rounded" />
              <div className="h-4 w-full bg-surface-container-high rounded" />
            </div>
          ))}
        </div>
      ) : isError ? (
        <div className="p-4"><ErrorAlert title={`${title} unavailable`} message={`${title} is temporarily unavailable.`} /></div>
      ) : feedItems.length === 0 ? (
        <EmptyState message="No news available." variant="minimal" className="min-h-32" />
      ) : (
        <div className="flex flex-col p-4 pt-0 flex-1 gap-4 min-h-0 overflow-y-auto custom-scrollbar px-4">
          {feedItems.map(item => (
            <div key={item.id} className="flex rounded-xl flex-col gap-2 p-4 bg-surface-container hover:bg-surface-container-high hover:m3-elevation-1 transition-colors">
              <div className="flex justify-between items-center mb-0.5">
                <a href={item.feedSource?.url || undefined} target="_blank" rel="noopener noreferrer" className="text-sm hover:underline underline-offset-2 font-black text-brand-btn-primary uppercase tracking-wide">
                  {item.feedSource?.name || 'Feed'}
                </a>
                <span className="text-sm text-on-surface-variant font-bold shrink-0">
                  {formatDateTime(item.publishDate)}
                </span>
              </div>
              <div className={"flex gap-2 justify-between items-center"}>
                <a href={item.link || undefined} target="_blank" rel="noopener noreferrer" className="underline-offset-2 text-left text-lg font-bold text-on-surface-variant leading-snug hover:underline transition-colors block">
                  {item.title || ''}
                </a>
                <ExternalLink size={18}/>
              </div>
            </div>
          ))}
        </div>
      )}
      {/*<div className="absolute bottom-0 left-0 right-0 h-16 bg-gradient-to-t from-white to-transparent pointer-events-none" />*/}
      {/*<div className="absolute top-8 left-0 right-0 h-6 bg-gradient-to-b from-white to-transparent pointer-events-none" />*/}
    </CollectionPanel>
  );
}

