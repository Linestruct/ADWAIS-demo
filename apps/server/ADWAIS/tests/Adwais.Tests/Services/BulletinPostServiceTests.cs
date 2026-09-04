// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Application.Common.Access;
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

        var service = new BulletinPostService(dbContext, OrgAccess(Guid.NewGuid()));
        var userId = Guid.NewGuid();

        // Act
        var result = await service.CreatePostAsync(userId, "Post Title", "Post Body", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("Post Title", result.Value.Title);
        Assert.Equal("Post Body", result.Value.Body);

        var saved = await dbContext.BulletinPosts.FindAsync(result.Value.Id);
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

        var service = new BulletinPostService(dbContext, PlatformAccess());

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

        var user = new User { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com" };
        var post1 = new BulletinPost { Id = Guid.NewGuid(), UserId = user.Id, Title = "Title 1", Body = "Body 1", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var post2 = new BulletinPost { Id = Guid.NewGuid(), UserId = user.Id, Title = "Title 2", Body = "Body 2", CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.BulletinPosts.AddRange(post1, post2);
        await dbContext.SaveChangesAsync();

        var service = new BulletinPostService(dbContext, PlatformAccess());

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

        var service = new BulletinPostService(dbContext, PlatformAccess());

        Assert.True((await service.DeletePostAsync(post.Id, CancellationToken.None)).IsSuccess);
        Assert.True((await service.DeletePostAsync(post.Id, CancellationToken.None)).IsFailed);
        Assert.Null(await dbContext.BulletinPosts.FindAsync(post.Id));
    }

    [Fact]
    public async Task CreatePostAsync_WithoutOrganizationScope_ReturnsScopeDenied()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);

        var service = new BulletinPostService(dbContext, PlatformAccess());

        var result = await service.CreatePostAsync(Guid.NewGuid(), "Title", "Body", CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsOnlyOrganizationPosts()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);
        var ownOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        var ownUser = new User { Id = Guid.NewGuid(), Name = "Own", Email = "own@example.com" };
        var otherUser = new User { Id = Guid.NewGuid(), Name = "Other", Email = "other@example.com" };
        var ownPost = new BulletinPost { Id = Guid.NewGuid(), UserId = ownUser.Id, Title = "Own", Body = "Body", CreatedAt = DateTime.UtcNow, OrganizationId = ownOrgId };
        var otherPost = new BulletinPost { Id = Guid.NewGuid(), UserId = otherUser.Id, Title = "Other", Body = "Body", CreatedAt = DateTime.UtcNow, OrganizationId = otherOrgId };
        dbContext.Users.AddRange(ownUser, otherUser);
        dbContext.BulletinPosts.AddRange(ownPost, otherPost);
        await dbContext.SaveChangesAsync();

        var service = new BulletinPostService(dbContext, OrgAccess(ownOrgId));

        var result = (await service.GetPostsAsync(CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal("Own", result[0].Title);
    }

    [Fact]
    public async Task CreatePostAsync_WithExplicitOrganizationId_AssignsGivenOrg()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);
        var targetOrgId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization { Id = targetOrgId, Name = "Target Org" });
        await dbContext.SaveChangesAsync();

        // An unscoped service (e.g., background / anonymous webhook context)
        var mockAccess = new Mock<ICurrentAccess>();
        mockAccess.Setup(a => a.Scope).Returns((AccessScope?)null);

        var service = new BulletinPostService(dbContext, mockAccess.Object);

        var result = await service.CreatePostAsync(
            AnalyticsDbContext.SystemUserGuid,
            "Webhook Title",
            "Webhook Body",
            targetOrgId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(targetOrgId, result.OrganizationId);
        Assert.Equal("Webhook Title", result.Title);
    }

    [Fact]
    public async Task CreatePostAsync_WithUnknownOrganization_ThrowsKeyNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        var dbContext = new AnalyticsDbContext(options);
        var unknownOrgId = Guid.NewGuid();

        var mockAccess = new Mock<ICurrentAccess>();
        mockAccess.Setup(a => a.Scope).Returns((AccessScope?)null);

        var service = new BulletinPostService(dbContext, mockAccess.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreatePostAsync(
            AnalyticsDbContext.SystemUserGuid,
            "Webhook Title",
            "Webhook Body",
            unknownOrgId,
            CancellationToken.None));
    }

    private static ICurrentAccess PlatformAccess()
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(null, null, [UserRole.Admin]));
        return mock.Object;
    }

    private static ICurrentAccess OrgAccess(Guid orgId)
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(orgId, null, [UserRole.Admin]));
        return mock.Object;
    }
}
