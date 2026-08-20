// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;

namespace Adwais.Infrastructure.Services.Parsers;

public class RssFeedParser : IFeedParser
{
    public bool CanParse(string url)
    {
        return url.Contains("format=rss") || url.EndsWith(".xml") || url.Contains("/rss");
    }

    public async Task<List<FeedItem>> ParseAsync(FeedSource source, HttpClient httpClient, CancellationToken ct)
    {
        var items = new List<FeedItem>();
        var response = await httpClient.GetAsync(source.Url, ct);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var xmlReader = XmlReader.Create(responseStream);
        var syndicationFeed = SyndicationFeed.Load(xmlReader);
        
        if (syndicationFeed != null)
        {
            foreach (var syncItem in syndicationFeed.Items)
            {
                var link = syncItem.Links.FirstOrDefault()?.Uri?.ToString() ?? syncItem.Id;
                if (string.IsNullOrEmpty(link)) continue;

                items.Add(new FeedItem
                {
                    Id = Guid.NewGuid(),
                    FeedSourceId = source.Id,
                    Title = syncItem.Title?.Text ?? "Untitled",
                    Content = syncItem.Summary?.Text ?? syncItem.Content?.ToString(),
                    Link = link,
                    PublishDate = syncItem.PublishDate.UtcDateTime,
                    Author = syncItem.Authors.FirstOrDefault()?.Name ?? source.Name
                });
            }
        }
        return items;
    }
}
