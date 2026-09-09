// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { X } from 'lucide-react';
import { createPortal } from 'react-dom';
import { Button } from '../ui/Button';

type AboutDemoModalProps = {
  isOpen: boolean;
  onClose: () => void;
};

export function AboutDemoModal({ isOpen, onClose }: AboutDemoModalProps) {
  if (!isOpen) return null;

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-2 backdrop-blur-sm animate-in fade-in sm:p-4"
      role="presentation"
      onMouseDown={event => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section
        className="flex max-h-[calc(100dvh-1rem)] w-full max-w-xl flex-col overflow-hidden rounded-3xl bg-surface m3-elevation-4 animate-in zoom-in-95 sm:max-h-[90vh]"
        role="dialog"
        aria-modal="true"
        aria-labelledby="about-demo-title"
      >
        <header className="flex shrink-0 items-center justify-between border-b border-outline-variant/40 px-4 py-3 sm:px-6 sm:py-5">
          <h2 id="about-demo-title" className="text-lg font-bold text-on-surface sm:text-xl">About this demo</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close about this demo"
            className="flex h-10 w-10 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary sm:h-11 sm:w-11"
          >
            <X size={20} />
          </button>
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 text-sm leading-relaxed text-on-surface-variant [overflow-wrap:anywhere] sm:px-6 sm:py-6 sm:text-base">
          <div className="flex flex-col gap-3 sm:gap-4">
            <p>
              This is an interactive demo of ADWAIS built by Olle Marmenlind. I&apos;m a final year Computer Engineering student working (for the past 5 years) as a Product Owner for{' '}
              <a className="text-brand-btn-primary underline" href="https://sevan.se/foretag" target="_blank" rel="noreferrer">Sevan.se</a>.
            </p>
            <p>All data is fictional and does not factually represent any storefronts in this demo.</p>
            <p>
              Originally created as an internal platform for{' '}
              <a className="text-brand-btn-primary underline" href="https://motillo.com" target="_blank" rel="noreferrer">Motillo.com</a>.
              It serves as an analytics dashboard of client storefronts (Shopify, Litium, etc.), a two-way control of their website monitoring tools (UptimeRobot, Uptime Kuma, etc.), and an intranet (bulletin board, shared calendar, and aggregation of internal and external articles/news).
            </p>
            <p>Today the schema supports multiple organizations within one deployment and has OpenTelemetry implemented with support for Grafana, .NET Aspire, and more.</p>
            <p>
              As I approach the conclusion of my degree I'll be transitioning into Software/Systems engineering. I&apos;m currently also looking for a company to host my graduate thesis anytime between January-May 2027. Know a person or company who&apos;d be willing to do so? Don&apos;t hesitate to reach out!
            </p>
            <ul className="flex list-disc flex-col gap-2 pl-5">
              <li>Learn more: <a className="text-brand-btn-primary underline" href="https://marmenlind.com/projects/adwais" target="_blank" rel="noreferrer">marmenlind.com/projects/adwais</a></li>
              <li>Source (MIT License): <a className="text-brand-btn-primary underline" href="https://github.com/sojupie/ADWAIS" target="_blank" rel="noreferrer">github.com/sojupie/ADWAIS</a></li>
              <li>Contact me: <a className="text-brand-btn-primary underline" href="mailto:olle@marmenlind.com">olle@marmenlind.com</a></li>
            </ul>
          </div>
        </div>

        <footer className="flex shrink-0 justify-end border-t border-outline-variant/40 px-4 py-3 sm:px-6 sm:py-4">
          <Button variant="tonal" color="primary" onClick={onClose}>Close</Button>
        </footer>
      </section>
    </div>,
    document.body,
  );
}
