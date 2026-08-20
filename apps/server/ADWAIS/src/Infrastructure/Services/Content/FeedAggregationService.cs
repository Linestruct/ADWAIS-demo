// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Services;

public class FeedAggregationService(
    IDbContextFactory<AnalyticsDbContext> contextFactory,
    IEnumerable<IFeedParser> parsers,
    HttpClient httpClient,
    ILogger<FeedAggregationService> logger,
    ISystemEventService eventService)
    : IFeedAggregationService
{
    private readonly IDbContextFactory<AnalyticsDbContext> _contextFactory = contextFactory;
    private readonly IEnumerable<IFeedParser> _parsers = parsers;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<FeedAggregationService> _logger = logger;
    private readonly ISystemEventService _eventService = eventService;

    public async Task AggregateAllFeedsAsync(CancellationToken ct = default)
    {
        List<FeedSource> sources;
        await using (var context = await _contextFactory.CreateDbContextAsync(ct))
        {
            sources = await context.FeedSources.Where(s => s.IsActive).ToListAsync(ct);
        }

        foreach (var source in sources)
        {
            try
            {
                await AggregateSourceAsync(source.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to aggregate feed source {Name}", source.Name);
                await _eventService.LogErrorAsync(nameof(FeedAggregationService), $"Feed aggregation failed for {source.Name}: {ex.Message}", ex);
            }
        }
    }

    public async Task AggregateSourceAsync(Guid sourceId, CancellationToken ct = default)
    {
        FeedSource? source;
        await using (var context = await _contextFactory.CreateDbContextAsync(ct))
        {
            source = await context.FeedSources.SingleOrDefaultAsync(s => s.Id == sourceId, ct);
        }

        if (source == null || !source.IsActive) return;

        try
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(source.Url));
            if (parser == null)
            {
                throw new NotSupportedException($"No registered parser could parse the URL: {source.Url}");
            }

            var incomingItems = await parser.ParseAsync(source, _httpClient, ct);
            if (incomingItems.Count > 0)
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);

                var existingItems = await context.FeedItems
                    .Where(fi => fi.FeedSourceId == source.Id)
                    .ToListAsync(ct);

                var existingByLink = existingItems.ToDictionary(fi => fi.Link, fi => fi, StringComparer.OrdinalIgnoreCase);
                var existingByTitle = existingItems.ToLookup(fi => fi.Title, fi => fi, StringComparer.OrdinalIgnoreCase);

                var itemsToProbe = new List<FeedItem>();
                foreach (var incomingItem in incomingItems)
                {
                    var normalizedLink = NormalizeFeedLink(incomingItem.Link);
                    if (existingByLink.TryGetValue(normalizedLink, out var matchedItem))
                    {
                        UpdateFeedItemFields(matchedItem, incomingItem);
                    }
                    else if (existingByTitle.Contains(incomingItem.Title))
                    {
                        matchedItem = existingByTitle[incomingItem.Title].First();
                        matchedItem.Link = normalizedLink;
                        UpdateFeedItemFields(matchedItem, incomingItem);
                        existingByLink[normalizedLink] = matchedItem;
                    }
                    else
                    {
                        itemsToProbe.Add(incomingItem);
                    }
                }

                if (itemsToProbe.Count > 0)
                {
                    var semaphore = new SemaphoreSlim(4);
                    try
                    {
                        var probeTasks = itemsToProbe.Select(async item =>
                        {
                            await semaphore.WaitAsync(ct);
                            try
                            {
                                return await ProbeUrlAsync(item.Link, ct);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }).ToList();
                        var probedItems = await Task.WhenAll(probeTasks);

                        for (int i = 0; i < itemsToProbe.Count; i++)
                        {
                            var incomingItem = itemsToProbe[i];
                            var probedItem = probedItems[i];

                            var canonicalUrl = NormalizeFeedLink(probedItem.CanonicalUrl);
                            var originalUrl = NormalizeFeedLink(probedItem.OriginalUrl);

                            if (existingByLink.TryGetValue(canonicalUrl, out var matchedItem))
                            {
                                UpdateFeedItemFields(matchedItem, incomingItem);
                            }
                            else if (probedItem.WasRedirected && existingByLink.TryGetValue(originalUrl, out matchedItem))
                            {
                                matchedItem.Link = canonicalUrl;
                                UpdateFeedItemFields(matchedItem, incomingItem);

                                existingByLink.Remove(originalUrl);
                                existingByLink[canonicalUrl] = matchedItem;
                            }
                            else if (existingByTitle.Contains(incomingItem.Title))
                            {
                                matchedItem = existingByTitle[incomingItem.Title].First();
                                matchedItem.Link = canonicalUrl;
                                UpdateFeedItemFields(matchedItem, incomingItem);
                                existingByLink[canonicalUrl] = matchedItem;
                            }
                            else
                            {
                                var newItem = new FeedItem
                                {
                                    Id = incomingItem.Id,
                                    Author = incomingItem.Author,
                                    Content = incomingItem.Content,
                                    FeedSourceId = source.Id,
                                    ImageUrl = incomingItem.ImageUrl,
                                    Link = canonicalUrl,
                                    PublishDate = incomingItem.PublishDate,
                                    Title = incomingItem.Title
                                };
                                context.FeedItems.Add(newItem);
                                existingByLink[canonicalUrl] = newItem;
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Dispose();
                    }
                }

                await context.SaveChangesAsync(ct);
            }

            await using (var context = await _contextFactory.CreateDbContextAsync(ct))
            {
                var src = await context.FeedSources.SingleAsync(s => s.Id == sourceId, ct);
                src.LastPolledAt = DateTime.UtcNow;
                src.LastSuccessAt = DateTime.UtcNow;
                src.LastSyncError = null;
                await context.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            await using (var context = await _contextFactory.CreateDbContextAsync(ct))
            {
                var src = await context.FeedSources.SingleAsync(s => s.Id == sourceId, ct);
                src.LastPolledAt = DateTime.UtcNow;
                src.LastSyncError = ex.Message;
                await context.SaveChangesAsync(ct);
            }
            await _eventService.LogErrorAsync(nameof(FeedAggregationService), $"Feed aggregation failed for {source.Name}: {ex.Message}", ex);
        }
    }

    private record ProbeResult(string OriginalUrl, string CanonicalUrl, bool WasRedirected);

    private async Task<ProbeResult> ProbeUrlAsync(string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return new ProbeResult(url, url, false);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request, cts.Token);

            var finalUri = response.RequestMessage?.RequestUri?.ToString() ?? url;
            return new ProbeResult(url, finalUri, url != finalUri);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve final URL for {Url}. Using original.", url);
            return new ProbeResult(url, url, false);
        }
    }

    private static void UpdateFeedItemFields(FeedItem existing, FeedItem incoming)
    {
        existing.Title = incoming.Title;
        existing.Author = incoming.Author;
        existing.Content = incoming.Content;
        existing.ImageUrl = incoming.ImageUrl;
    }

    private static string NormalizeFeedLink(string link)
    {
        var normalized = link.Trim();

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty
            };

            normalized = builder.Uri.ToString();
        }

        return normalized.TrimEnd('/');
    }
}
