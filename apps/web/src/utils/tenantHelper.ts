// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

const domainCache = new Map<string, string | null>();
const faviconCache = new Map<string, string | null>();

/**
 * Derives a best-guess domain from a tenant's base URL.
 * Strips the www prefix and ignores internal/mock hosts.
 */
export function getTenantDomain(url?: string | null): string | null {
  const cacheKey = url ?? '';
  if (domainCache.has(cacheKey)) return domainCache.get(cacheKey) ?? null;

  let resolved: string | null = null;
  if (url) {
    const trimmed = url.trim();
    try {
      const parsed = new URL(trimmed);
      const hostname = parsed.hostname.replace(/^www\./, '');
      const isInternal = !hostname.includes('.') || hostname.endsWith('.mock');
      if (!isInternal) resolved = hostname;
    } catch {
      // No scheme — try treating it as a bare hostname
      const bare = trimmed.replace(/^www\./, '').split('/')[0];
      const isInternal = !bare.includes('.') || bare.endsWith('.mock');
      if (!isInternal && /^[a-z0-9.-]+\.[a-z]{2,}$/i.test(bare)) {
        resolved = bare;
      }
    }
  }

  domainCache.set(cacheKey, resolved);
  return resolved;
}

/**
 * Returns a Google favicon URL for the given tenant base URL,
 * or null if no resolvable domain is available.
 */
export function getTenantFaviconUrl(url?: string | null): string | null {
  const cacheKey = url ?? '';
  if (faviconCache.has(cacheKey)) return faviconCache.get(cacheKey) ?? null;

  const domain = getTenantDomain(url);
  const faviconUrl = domain
    ? `https://www.google.com/s2/favicons?domain=${domain}&sz=128`
    : null;

  faviconCache.set(cacheKey, faviconUrl);
  return faviconUrl;
}
