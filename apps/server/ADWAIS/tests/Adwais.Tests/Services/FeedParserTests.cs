// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities.Intranet;
using Adwais.Infrastructure.Services.Parsers;
using Xunit;

namespace Adwais.Tests.Services;

public class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sendFunc) : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _sendFunc = sendFunc;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_sendFunc(request));
    }
}

public class FeedParserTests
{
    [Fact]
    public async Task RssFeedParser_CanParse_RssUrl_AndExtractsItems()
    {
        // Arrange
        var parser = new RssFeedParser();
        var source = new FeedSource { Id = Guid.NewGuid(), Name = "RSS", Url = "https://news.cision.com/rss", IsActive = true };
        
        var rssXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <rss version=""2.0"">
          <channel>
            <item>
              <title>Test RSS Title</title>
              <link>https://news.cision.com/rss-test</link>
              <pubDate>Tue, 23 Jun 2026 12:00:00 GMT</pubDate>
              <description>RSS description</description>
            </item>
          </channel>
        </rss>";
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(rssXml, Encoding.UTF8, "application/xml") });
        var client = new HttpClient(handler);

        // Act & Assert
        Assert.True(parser.CanParse(source.Url));
        var items = await parser.ParseAsync(source, client, CancellationToken.None);
        var item = Assert.Single(items);
        Assert.Equal("Test RSS Title", item.Title);
        Assert.Equal("https://news.cision.com/rss-test", item.Link);
    }

    [Fact]
    public async Task LitiumBlogParser_CanParse_LitiumBlog_AndScrapesItems()
    {
        // Arrange
        var parser = new LitiumBlogParser();
        var source = new FeedSource { Id = Guid.NewGuid(), Name = "Litium Blog", Url = "https://www.litium.com/blog", IsActive = true };
        
        var html = @"
        <html>
          <body>
            <article class=""blog-index-container"">
              <a class=""blog-index-link"" href=""/blog/customer-pricing"">
                <div class=""blog-item-top-container"" style=""background-image: url(https://litium.com/image.jpg);""></div>
              </a>
              <h3>Customer Pricing</h3>
              <span class=""publish-date"">23/06/2026</span>
            </article>
          </body>
        </html>";
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") });
        var client = new HttpClient(handler);

        // Act & Assert
        Assert.True(parser.CanParse(source.Url));
        var items = await parser.ParseAsync(source, client, CancellationToken.None);
        var item = Assert.Single(items);
        Assert.Equal("Customer Pricing", item.Title);
        Assert.Equal("https://www.litium.com/blog/customer-pricing", item.Link);
        Assert.Equal("https://litium.com/image.jpg", item.ImageUrl);
    }

    [Fact]
    public async Task LitiumBlogParser_CanParse_SwedishRapporter_AndScrapesItems()
    {
        // Arrange
        var parser = new LitiumBlogParser();
        var source = new FeedSource { Id = Guid.NewGuid(), Name = "Litium Rapporter", Url = "https://www.litium.se/insikter/rapporter-guider", IsActive = true };
        
        var html = @"
        <article class=""visible-posts blog-index-container span4"">
          <a href=""https://www.litium.se/b2b-rapport"">
            <div class=""blog-item-top-container blog-item-top-container--img"">
            <img src=""https://www.litium.se/hs-fs/hubfs/B2B_report_2026.jpg?width=800"" alt=""Ny rapport"">
            </div>
          </a>
          <div class=""blog-index-text-container"">
            <a href=""https://www.litium.se/b2b-rapport"" class=""blog-index-link"">
              <h3 class=""heading-m"">Ny rapport: Nordic Digital Commerce in B2B 2026</h3>
              <p>För tionde året i rad publicerar Litium en rapport</p>
            </a>
          </div>
        </article>";
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") });
        var client = new HttpClient(handler);

        // Act & Assert
        Assert.True(parser.CanParse(source.Url));
        var items = await parser.ParseAsync(source, client, CancellationToken.None);
        var item = Assert.Single(items);
        Assert.Equal("Ny rapport: Nordic Digital Commerce in B2B 2026", item.Title);
        Assert.Equal("https://www.litium.se/b2b-rapport", item.Link);
        Assert.Equal("https://www.litium.se/hs-fs/hubfs/B2B_report_2026.jpg?width=800", item.ImageUrl);
        Assert.Equal("För tionde året i rad publicerar Litium en rapport", item.Content);
    }

    [Fact]
    public async Task LitiumNyhetsrumParser_CanParse_Nyhetsrum_AndScrapesItems()
    {
        // Arrange
        var parser = new LitiumNyhetsrumParser();
        var source = new FeedSource { Id = Guid.NewGuid(), Name = "Litium Nyhetsrum", Url = "https://www.litium.se/nyhetsrum", IsActive = true };
        
        var html = @"
        <div id=""container"">
          <div class=""mfn-content"">
            <div class=""mfn-row"">
              <div class=""mfn-primary-image"">
                <a href=""/nyhetsrum?slug=styrelse-och-ledning-okar-sitt-agande-i-litium"">
                  <div class=""mfn-image-placeholder""></div>
                </a>
              </div>
              <div class=""mfn-date"">
                <span class=""mfn-timestamp-date"">9 juni 2026</span>
                <span class=""mfn-timestamp-separator"">&nbsp;</span>
                <span class=""mfn-timestamp-time"">09:05</span>
              </div>
              <div class=""mfn-title-wrapper"">
                <a href=""/nyhetsrum?slug=styrelse-och-ledning-okar-sitt-agande-i-litium"" class=""mfn-title"">Styrelse och ledning ökar sitt ägande i Litium</a>
              </div>
              <div class=""mfn-preamble"">Litium AB meddelar att flera personer...</div>
            </div>
          </div>
        </div>";
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") });
        var client = new HttpClient(handler);

        // Act & Assert
        Assert.True(parser.CanParse(source.Url));
        var items = await parser.ParseAsync(source, client, CancellationToken.None);
        var item = Assert.Single(items);
        Assert.Equal("Styrelse och ledning ökar sitt ägande i Litium", item.Title);
        Assert.Equal("https://www.litium.se/nyhetsrum?slug=styrelse-och-ledning-okar-sitt-agande-i-litium", item.Link);
        Assert.Null(item.ImageUrl);
        Assert.Equal("Litium AB meddelar att flera personer...", item.Content);
        Assert.Equal(new DateTime(2026, 6, 9, 9, 5, 0, DateTimeKind.Utc), item.PublishDate);
    }

    [Fact]
    public async Task MotilloAktuelltParser_CanParse_MotilloUrl_AndScrapesItems()
    {
        // Arrange
        var parser = new MotilloAktuelltParser();
        var source = new FeedSource { Id = Guid.NewGuid(), Name = "Motillo", Url = "https://www.motillo.com/sv/aktuellt", IsActive = true };
        
        var html = @"
        <html>
          <body>
            <a class=""group"" href=""/aktuellt/next-gen"">
              <h3>Next Gen Commerce</h3>
              <p>Scraped summary content</p>
              <img src=""/image-path.jpg"" />
            </a>
          </body>
        </html>";
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") });
        var client = new HttpClient(handler);

        // Act & Assert
        Assert.True(parser.CanParse(source.Url));
        var items = await parser.ParseAsync(source, client, CancellationToken.None);
        var item = Assert.Single(items);
        Assert.Equal("Next Gen Commerce", item.Title);
        Assert.Equal("https://www.motillo.com/aktuellt/next-gen", item.Link);
        Assert.Equal("https://www.motillo.com/image-path.jpg", item.ImageUrl);
        Assert.Equal("Scraped summary content", item.Content);
    }
}
