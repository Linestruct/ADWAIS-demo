// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class BulletinPostServiceTests
{
    [Fact]
    public async Task CreatePostAsync_SavesPostToDatabase_AndReturnsEntity()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);

        var service = new BulletinPostService(dbContext);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.CreatePostAsync(userId, "Post Title", "Post Body", CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Post Title", result.Title);
        Assert.Equal("Post Body", result.Body);

        var saved = await dbContext.BulletinPosts.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Post Title", saved.Title);
    }

    [Fact]
    public async Task GetPostByIdAsync_ReturnsCorrectPostOrNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);

        var post = new BulletinPost { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "Title", Body = "Body", CreatedAt = DateTime.UtcNow };
        dbContext.BulletinPosts.Add(post);
        await dbContext.SaveChangesAsync();

        var service = new BulletinPostService(dbContext);

        // Act
        var result = await service.GetPostByIdAsync(post.Id, CancellationToken.None);
        var nonExistent = await service.GetPostByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Title", result.Title);
        Assert.Null(nonExistent);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsAllPosts_SortedByDateDescending_WithUserDetails()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);

        var user = new User { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com", Role = UserRole.Employee };
        var post1 = new BulletinPost { Id = Guid.NewGuid(), UserId = user.Id, Title = "Title 1", Body = "Body 1", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var post2 = new BulletinPost { Id = Guid.NewGuid(), UserId = user.Id, Title = "Title 2", Body = "Body 2", CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.BulletinPosts.AddRange(post1, post2);
        await dbContext.SaveChangesAsync();

        var service = new BulletinPostService(dbContext);

        // Act
        var result = (await service.GetPostsAsync(CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Title 2", result[0].Title); // sorted newer first
        Assert.Equal("Title 1", result[1].Title);
        Assert.NotNull(result[0].User);
        Assert.Equal("John Doe", result[0].User.Name);
    }

    [Fact]
    public async Task DeletePostAsync_RemovesPost_AndReturnsFalseWhenMissing()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Title",
            Body = "Body",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.BulletinPosts.Add(post);
        await dbContext.SaveChangesAsync();

        var service = new BulletinPostService(dbContext);

        Assert.True(await service.DeletePostAsync(post.Id, CancellationToken.None));
        Assert.False(await service.DeletePostAsync(post.Id, CancellationToken.None));
        Assert.Null(await dbContext.BulletinPosts.FindAsync(post.Id));
    }
}
