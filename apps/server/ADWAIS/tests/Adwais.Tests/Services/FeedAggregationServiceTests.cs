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
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Adwais.Tests.Services;

public class FeedAggregationServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _options;
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly Mock<ILogger<FeedAggregationService>> _loggerMock;

    public FeedAggregationServiceTests()
    {
        // A single database instance per test class run, identified by a unique name.
        var dbName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        _eventServiceMock = new Mock<ISystemEventService>();
        _loggerMock = new Mock<ILogger<FeedAggregationService>>();
    }

    private Mock<IDbContextFactory<AnalyticsDbContext>> GetDbContextFactoryMock()
    {
        var factoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        // This is the key: return a NEW context instance each time, but with the SAME database options.
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(_options));
        return factoryMock;
    }
    
    [Fact]
    public async Task AggregateSourceAsync_WithNewItem_ShouldCreateNewItem()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        const string itemLink = "https://test.com/new-article";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            await context.SaveChangesAsync();
        }

        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { new() { Title = "New Item", Link = itemLink, Id = Guid.NewGuid() } });

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage(HttpMethod.Head, itemLink) });
        var httpClient = new HttpClient(mockHttpHandler.Object);

        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var item = await assertContext.FeedItems.SingleOrDefaultAsync();
        Assert.NotNull(item);
        Assert.Equal("New Item", item.Title);
        Assert.Equal(itemLink, item.Link);
    }

    [Fact]
    public async Task AggregateSourceAsync_WithUpdatedTitle_ShouldUpdateExistingItem()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string itemLink = "https://test.com/article-1";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            context.FeedItems.Add(new FeedItem { Id = itemId, FeedSourceId = sourceId, Title = "Old Title", Link = itemLink });
            await context.SaveChangesAsync();
        }

        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { new() { Title = "New Title", Link = itemLink, Id = Guid.NewGuid() } });

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage(HttpMethod.Head, itemLink) });
        var httpClient = new HttpClient(mockHttpHandler.Object);

        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var items = await assertContext.FeedItems.ToListAsync();
        var item = Assert.Single(items);
        Assert.Equal("New Title", item.Title);
        Assert.Equal(itemId, item.Id);
    }
    
    [Fact]
    public async Task AggregateSourceAsync_WithNewUrlAndSameTitle_ShouldUpdateExistingItem()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string oldLink = "https://test.com/article-1";
        const string newLink = "https://test.com/article-1-new";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            context.FeedItems.Add(new FeedItem { Id = itemId, FeedSourceId = sourceId, Title = "Same Title", Link = oldLink });
            await context.SaveChangesAsync();
        }
        
        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { new() { Title = "Same Title", Link = newLink, Id = Guid.NewGuid() } });
        
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage(HttpMethod.Head, newLink) });
        var httpClient = new HttpClient(mockHttpHandler.Object);
        
        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var item = await assertContext.FeedItems.SingleAsync();
        Assert.Equal(newLink, item.Link);
        Assert.Equal(itemId, item.Id);
    }

    [Fact]
    public async Task AggregateSourceAsync_WithRedirectToExistingLink_ShouldUpdateExistingItem()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string oldUrl = "https://test.com/old-article";
        const string newUrl = "https://test.com/new-article";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            context.FeedItems.Add(new FeedItem { Id = itemId, FeedSourceId = sourceId, Title = "Old Title", Link = oldUrl });
            await context.SaveChangesAsync();
        }

        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { new() { Title = "New Title", Link = newUrl, Id = Guid.NewGuid() } });

        var mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == newUrl), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.MovedPermanently) { Headers = { Location = new Uri(oldUrl) }, RequestMessage = new HttpRequestMessage(HttpMethod.Head, oldUrl) });
        
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var item = await assertContext.FeedItems.SingleAsync();
        Assert.Equal("New Title", item.Title);
        Assert.Equal(oldUrl, item.Link);
        Assert.Equal(itemId, item.Id);
    }

    [Fact]
    public async Task AggregateSourceAsync_WithDuplicateIncomingItems_ShouldNotThrowAndShouldInsertOnlyOne()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        const string itemLink = "https://test.com/duplicate-article";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            await context.SaveChangesAsync();
        }

        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem>
            {
                new() { Title = "First Title", Link = itemLink, Id = Guid.NewGuid() },
                new() { Title = "Second Title (Duplicate)", Link = itemLink, Id = Guid.NewGuid() }
            });

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage(HttpMethod.Head, itemLink) });
        var httpClient = new HttpClient(mockHttpHandler.Object);

        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var items = await assertContext.FeedItems.Where(fi => fi.FeedSourceId == sourceId).ToListAsync();
        var item = Assert.Single(items);
        Assert.Equal("Second Title (Duplicate)", item.Title);
        Assert.Equal(itemLink, item.Link);
    }

    [Fact]
    public async Task AggregateSourceAsync_ShouldSynchronizeAllFeedItemFields()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string itemLink = "https://test.com/sync-article";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            context.FeedItems.Add(new FeedItem
            {
                Id = itemId,
                FeedSourceId = sourceId,
                Title = "Old Title",
                Link = itemLink,
                Author = "Old Author",
                Content = "Old Content",
                ImageUrl = "https://test.com/old.png",
                PublishDate = DateTime.UnixEpoch
            });
            await context.SaveChangesAsync();
        }

        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        
        var now = DateTime.UtcNow;
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem>
            {
                new()
                {
                    Title = "New Title",
                    Link = itemLink,
                    Author = "New Author",
                    Content = "New Content",
                    ImageUrl = "https://test.com/new.png",
                    PublishDate = now
                }
            });

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage(HttpMethod.Head, itemLink) });
        var httpClient = new HttpClient(mockHttpHandler.Object);

        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var items = await assertContext.FeedItems.Where(fi => fi.FeedSourceId == sourceId).ToListAsync();
        var item = Assert.Single(items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal("New Title", item.Title);
        Assert.Equal("New Author", item.Author);
        Assert.Equal("New Content", item.Content);
        Assert.Equal("https://test.com/new.png", item.ImageUrl);
    }

    [Fact]
    public async Task AggregateSourceAsync_WithExistingItemByLink_ShouldSkipProbing()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string itemLink = "https://test.com/existing-article";
        await using (var context = new AnalyticsDbContext(_options))
        {
            context.FeedSources.Add(new FeedSource { Id = sourceId, Name = "Test Source", Url = "https://test.com", IsActive = true });
            context.FeedItems.Add(new FeedItem { Id = itemId, FeedSourceId = sourceId, Title = "Old Title", Link = itemLink });
            await context.SaveChangesAsync();
        }

        var factoryMock = GetDbContextFactoryMock();
        var parserMock = new Mock<IFeedParser>();
        parserMock.Setup(p => p.CanParse(It.IsAny<string>())).Returns(true);
        parserMock.Setup(p => p.ParseAsync(It.IsAny<FeedSource>(), It.IsAny<HttpClient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { new() { Title = "New Title", Link = itemLink, Id = Guid.NewGuid() } });

        // Strict behavior ensures that if SendAsync is called on this handler, it throws an exception.
        var mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(mockHttpHandler.Object);

        var service = new FeedAggregationService(factoryMock.Object, new[] { parserMock.Object }, httpClient, _loggerMock.Object, _eventServiceMock.Object);

        // Act
        // This will throw if the HttpClient is invoked (probing is executed)
        await service.AggregateSourceAsync(sourceId);

        // Assert
        await using var assertContext = new AnalyticsDbContext(_options);
        var item = await assertContext.FeedItems.SingleAsync(fi => fi.Id == itemId);
        Assert.Equal("New Title", item.Title); // Verifies that it successfully matched and updated without probing!
    }
}
