// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { ExternalLink } from 'lucide-react';
import { CollectionPanel } from '../common/dashboard/CollectionPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { ErrorAlert } from '../common/ui/ErrorAlert';
import { useGetApiIntranetFeeds } from '../../api/generated/endpoints';
import { formatDateTime } from '../../utils/dateTime';

type OrganizationNewsProps = {
  authorName: string;
  title?: string;
};

export function OrganizationNews({ authorName, title = 'Organization news' }: OrganizationNewsProps) {
  const { data: response, isLoading, isError } = useGetApiIntranetFeeds({ PageSize: 20, AuthorName: authorName });
  const feedItems = response?.data || [];

  const [selectedPostId, setSelectedPostId] = useState<string | undefined>(undefined);

  const selectedPost = feedItems.find((p) => p.id === selectedPostId) || feedItems[0];

  return (
    <CollectionPanel title={title} className="h-full relative" titleClassName={""}>
      {isLoading ? (
        <div className="flex flex-col md:flex-row h-full animate-pulse">
          {/* Skeleton Sidebar */}
          <div className="flex flex-row md:flex-col w-full md:w-1/3 flex-shrink-0 p-4 gap-8 overflow-hidden">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex flex-col gap-4 p-4 w-[280px] md:w-auto flex-shrink-0 rounded-xl bg-surface-container/50">
                <div className="h-3 w-20 bg-surface-container-high rounded-full" />
                <div className="h-4 w-full bg-surface-container-high rounded-full" />
                <div className="h-4 w-2/3 bg-surface-container-high rounded-full" />
              </div>
            ))}
          </div>
          {/* Skeleton Detail */}
          <div className="flex-1 flex flex-col p-6 gap-8">
            <div className="h-6 w-3/4 bg-surface-container-high rounded" />
            <div className="h-3 w-1/4 bg-surface-container-high rounded" />
            <div className="h-20 w-full bg-surface-container-high rounded" />
            <div className="w-full flex-1 min-h-[150px] bg-surface-container-high rounded-lg" />
          </div>
        </div>
      ) : isError ? (
        <div className="p-4"><ErrorAlert title={`${title} unavailable`} message={`${title} is temporarily unavailable.`} /></div>
      ) : feedItems.length === 0 ? (
        <EmptyState message="No news available." variant="minimal" className="min-h-32" />
      ) : (
        <div className="flex gap-2 p-4 pt-0 flex-col md:flex-row h-full overflow-hidden">
          {/* Sidebar List (Surface Container Low) */}
          <div className="flex flex-row md:flex-col rounded-2xl w-full md:w-1/3 md:h-full flex-shrink-0 p-3 gap-4 overflow-x-auto md:overflow-x-hidden md:overflow-y-auto custom-scrollbar bg-surface-container snap-x scroll-px-4">
            {feedItems.map((post) => {
              const isSelected = selectedPost?.id === post.id;
              return (
                <button
                  key={post.id}
                  onClick={() => setSelectedPostId(post.id)}
                  className={`flex min-h-20 flex-col justify-center text-left p-4 rounded-xl transition-all w-[280px] md:w-auto flex-shrink-0 snap-start ${
                    isSelected ? 'bg-primary-container text-on-primary-container shadow-sm' : 'bg-transparent hover:bg-surface-container-high'
                  }`}
                >
                  <div className="flex items-center gap-4 mb-2">
                    <span className={`text-sm font-bold tracking-wide ${isSelected ? 'text-on-primary-container' : 'text-on-surface-variant'}`}>
                      {formatDateTime(post.publishDate)}
                    </span>
                  </div>
                  <p className={`text-lg leading-snug transition-colors ${isSelected ? 'text-on-primary-container font-bold' : 'text-on-surface-variant font-semibold'}`}>
                    {post.title}
                  </p>
                </button>
              );
            })}
          </div>

          {/* Detail View (Surface) */}
          <div className="flex-1 flex flex-col p-5 md:p-8 min-h-0 overflow-y-auto custom-scrollbar bg-surface-container rounded-2xl">
            {selectedPost && (
              <>
                <div className="flex flex-col gap-2 mb-4 shrink-0">
                  <a
                      href={selectedPost.link || undefined}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-2xl md:text-3xl font-black text-brand-text leading-tight hover:text-brand-link hover:underline underline-offset-2"
                  >
                    {selectedPost.title}
                  </a>
                  <div className="text-base text-on-surface-variant font-bold uppercase tracking-wide">
                    {formatDateTime(selectedPost.publishDate)}
                    {selectedPost.author ? ` • ${selectedPost.author}` : ''}
                    {selectedPost.feedSource?.name && (
                      <>
                        {' • '}
                        <a
                          href={selectedPost.feedSource.url || undefined}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="hover:underline underline-offset-2"
                        >
                          {selectedPost.feedSource.name}
                        </a>
                      </>
                    )}
                  </div>
                </div>

                {selectedPost.content && (
                  <p className="text-base text-brand-text mb-6 leading-relaxed whitespace-pre-line">
                    {selectedPost.content}
                  </p>
                )}

                {selectedPost.imageUrl && (
                  <div className="w-full flex-1 min-h-[200px] max-h-[400px] rounded-xl overflow-hidden relative shrink-0 animate-in fade-in duration-300 mb-4">
                    <img
                      src={selectedPost.imageUrl}
                      className="w-full h-full object-cover"
                      alt={selectedPost.title || 'News image'}
                    />
                  </div>
                )}
                
                {selectedPost.link && (
                  <div className="mt-2 shrink-0">
                    <a
                      data-md3-ripple
                      href={selectedPost.link}
                      target="_blank"
                      rel="noopener noreferrer"
                    className="inline-flex min-h-11 items-center justify-center gap-2 rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
                    >
                    Read full article
                      <ExternalLink size={16} aria-hidden="true" />
                    </a>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </CollectionPanel>
  );
}
