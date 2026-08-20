// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using HtmlAgilityPack;

namespace Adwais.Infrastructure.Services.Parsers;

public class LitiumBlogParser : IFeedParser
{
    public bool CanParse(string url) => (url.Contains("litium.com") || url.Contains("litium.se")) && !url.Contains("nyhetsrum");

    public async Task<List<FeedItem>> ParseAsync(FeedSource source, HttpClient httpClient, CancellationToken ct)
    {
        var items = new List<FeedItem>();
        var html = await httpClient.GetStringAsync(source.Url, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var baseUri = new Uri(source.Url);
        var host = baseUri.GetLeftPart(UriPartial.Authority);

        var articleNodes = doc.DocumentNode.SelectNodes("//article[contains(@class, 'blog-index-container')]");
        if (articleNodes != null)
        {
            foreach (var node in articleNodes)
            {
                var titleNode = node.SelectSingleNode(".//h3");
                var title = titleNode?.InnerText?.Trim() ?? "Untitled";

                var linkNode = node.SelectSingleNode(".//a");
                var link = linkNode?.GetAttributeValue("href", "") ?? "";
                if (link.StartsWith("/")) link = host + link;

                string? imageUrl = null;
                var imgContainer = node.SelectSingleNode(".//div[contains(@class, 'blog-item-top-container')]");
                if (imgContainer != null)
                {
                    var imgNode = imgContainer.SelectSingleNode(".//img");
                    if (imgNode != null)
                    {
                        imageUrl = imgNode.GetAttributeValue("src", "");
                    }
                    else
                    {
                        var style = imgContainer.GetAttributeValue("style", "");
                        var match = Regex.Match(style, @"url\(['""]?(.*?)['""]?\)");
                        if (match.Success) imageUrl = match.Groups[1].Value.Trim('\'', '"');
                    }
                }

                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("/"))
                {
                    imageUrl = host + imageUrl;
                }

                var dateNode = node.SelectSingleNode(".//span[contains(@class, 'publish-date')]");
                var date = DateTime.UtcNow;
                if (dateNode != null && DateTime.TryParseExact(dateNode.InnerText.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate))
                {
                    date = parsedDate.ToUniversalTime();
                }

                var contentNode = node.SelectSingleNode(".//p");
                var content = contentNode?.InnerText?.Trim() ?? title;

                items.Add(new FeedItem
                {
                    Id = Guid.NewGuid(),
                    FeedSourceId = source.Id,
                    Title = title,
                    Link = link,
                    PublishDate = date,
                    ImageUrl = imageUrl,
                    Content = content,
                    Author = "Litium"
                });
            }
        }
        return items;
    }
}
