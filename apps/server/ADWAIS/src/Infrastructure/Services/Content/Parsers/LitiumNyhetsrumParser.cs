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

public class LitiumNyhetsrumParser : IFeedParser
{
    public bool CanParse(string url) => url.Contains("litium") && url.Contains("nyhetsrum");

    public async Task<List<FeedItem>> ParseAsync(FeedSource source, HttpClient httpClient, CancellationToken ct)
    {
        var items = new List<FeedItem>();
        var html = await httpClient.GetStringAsync(source.Url, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var baseUri = new Uri(source.Url);
        var host = baseUri.GetLeftPart(UriPartial.Authority);

        var rowNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'mfn-row')]");
        if (rowNodes != null)
        {
            foreach (var node in rowNodes)
            {
                var titleNode = node.SelectSingleNode(".//a[contains(@class, 'mfn-title')]") 
                    ?? node.SelectSingleNode(".//div[contains(@class, 'mfn-title-wrapper')]/a");
                var title = titleNode?.InnerText?.Trim() ?? "Untitled";

                var link = titleNode?.GetAttributeValue("href", "") ?? "";
                if (link.StartsWith("/")) link = host + link;

                string? imageUrl = null;
                var imgNode = node.SelectSingleNode(".//div[contains(@class, 'mfn-primary-image')]//img");
                if (imgNode != null)
                {
                    imageUrl = imgNode.GetAttributeValue("src", "");
                }
                else
                {
                    var img = node.SelectSingleNode(".//img");
                    if (img != null)
                    {
                        imageUrl = img.GetAttributeValue("src", "");
                    }
                }

                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("/"))
                {
                    imageUrl = host + imageUrl;
                }

                var dateSpan = node.SelectSingleNode(".//span[contains(@class, 'mfn-timestamp-date')]");
                var timeSpan = node.SelectSingleNode(".//span[contains(@class, 'mfn-timestamp-time')]");
                var date = DateTime.UtcNow;

                if (dateSpan != null)
                {
                    date = ParseSwedishDate(dateSpan.InnerText.Trim(), timeSpan?.InnerText?.Trim() ?? "");
                }

                var preambleNode = node.SelectSingleNode(".//div[contains(@class, 'mfn-preamble')]");
                var content = preambleNode?.InnerText?.Trim() ?? title;

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

    private static DateTime ParseSwedishDate(string dateStr, string timeStr)
    {
        var parts = dateStr.ToLowerInvariant().Replace(".", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return DateTime.UtcNow;

        if (!int.TryParse(parts[0], out var day)) return DateTime.UtcNow;
        if (!int.TryParse(parts[2], out var year)) return DateTime.UtcNow;

        var monthName = parts[1];
        int month = monthName switch
        {
            "jan" or "januari" => 1,
            "feb" or "februari" => 2,
            "mar" or "mars" => 3,
            "apr" or "april" => 4,
            "maj" => 5,
            "jun" or "juni" => 6,
            "jul" or "juli" => 7,
            "aug" or "augusti" => 8,
            "sep" or "september" => 9,
            "okt" or "oktober" => 10,
            "nov" or "november" => 11,
            "dec" or "december" => 12,
            _ => 1
        };

        int hour = 0, minute = 0;
        if (!string.IsNullOrEmpty(timeStr))
        {
            var timeParts = timeStr.Split(':');
            if (timeParts.Length == 2 && int.TryParse(timeParts[0], out var h) && int.TryParse(timeParts[1], out var m))
            {
                hour = h;
                minute = m;
            }
        }

        return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    }
}
