// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using HtmlAgilityPack;

namespace Adwais.Infrastructure.Services.Parsers;

public class MotilloAktuelltParser : IFeedParser
{
    public bool CanParse(string url) => url.Contains("motillo.com");

    public async Task<List<FeedItem>> ParseAsync(FeedSource source, HttpClient httpClient, CancellationToken ct)
    {
        var items = new List<FeedItem>();
        var html = await httpClient.GetStringAsync(source.Url, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'group') and contains(@href, '/aktuellt/')]");
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                var link = node.GetAttributeValue("href", "");
                if (link.StartsWith("/")) link = "https://www.motillo.com" + link;

                var titleNode = node.SelectSingleNode(".//h3");
                var title = titleNode?.InnerText?.Trim() ?? "Untitled";

                var contentNode = node.SelectSingleNode(".//p");
                var content = contentNode?.InnerText?.Trim() ?? "";

                var imgNode = node.SelectSingleNode(".//img");
                var imageUrl = imgNode?.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("/")) imageUrl = "https://www.motillo.com" + imageUrl;

                items.Add(new FeedItem
                {
                    Id = Guid.NewGuid(),
                    FeedSourceId = source.Id,
                    Title = title,
                    Link = link,
                    PublishDate = DateTime.UtcNow,
                    ImageUrl = imageUrl,
                    Content = content,
                    Author = "Motillo"
                });
            }
        }
        return items;
    }
}
