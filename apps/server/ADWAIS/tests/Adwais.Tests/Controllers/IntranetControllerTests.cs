// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Content;
using Adwais.Api.Controllers.Integrations;
using Adwais.Api.Controllers.System;
using Adwais.Api.DTOs.Intranet;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using Adwais.Infrastructure.Persistence;

namespace Adwais.Tests.Controllers;

public class IntranetControllerTests
{
    private readonly Mock<ISystemHealthService> _healthServiceMock;
    private readonly SystemHealthController _controller;

    public IntranetControllerTests()
    {
        _healthServiceMock = new Mock<ISystemHealthService>();
        _controller = new SystemHealthController(_healthServiceMock.Object);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOkWithSystemHealthDto()
    {
        // Arrange
        var healthDto = new SystemHealthDto(
            DatabaseStatus: "Healthy",
            Hangfire: new HangfireHealthDto("Healthy", 0, 0, 0, 0),
            Sync: new SyncHealthDto("Healthy", 0, 0, 0, null),
            LastLitiumSync: null,
            LastBlogSync: null,
            LastFleetUpdate: null,
            LastFleetUptimeUpdate: null,
            LastFleetLatencyUpdate: null
        );

        _healthServiceMock.Setup(s => s.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthDto);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHealth = Assert.IsType<SystemHealthDto>(okResult.Value);
        Assert.Equal("Healthy", returnedHealth.DatabaseStatus);
        _healthServiceMock.Verify(s => s.GetHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearErrors_ShouldReturnNoContent()
    {
        // Arrange & Act
        var result = await _controller.ClearErrors();

        // Assert
        Assert.IsType<NoContentResult>(result);
        _healthServiceMock.Verify(s => s.ClearErrorsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhooks_ReceiveBulletinPost_ShouldReturnOk_AndSaveToDb_WhenApiKeyIsValid()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Webhooks:BulletinPostApiKey"]).Returns("valid-secret-key");

        var createdPost = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = AnalyticsDbContext.SystemUserGuid,
            Title = "Weekly News",
            Body = "Content",
            CreatedAt = DateTime.UtcNow
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.CreatePostAsync(AnalyticsDbContext.SystemUserGuid, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPost);

        var controller = new WebhooksController(
            new Mock<IOrderIngestionService>().Object,
            configMock.Object,
            new Mock<ILogger<WebhooksController>>().Object,
            postServiceMock.Object);

        var payload = new CreateBulletinPostWebhookRequest { Title = "Weekly News", Body = "Content" };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Api-Key"] = "valid-secret-key";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.ReceiveBulletinPost(payload, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = okResult.Value;
        Assert.NotNull(returnedData);
        postServiceMock.Verify(s => s.CreatePostAsync(AnalyticsDbContext.SystemUserGuid, payload.Title, payload.Body, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhooks_ReceiveBulletinPost_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Webhooks:BulletinPostApiKey"]).Returns("valid-secret-key");

        var postServiceMock = new Mock<IBulletinPostService>();
        var controller = new WebhooksController(
            new Mock<IOrderIngestionService>().Object,
            configMock.Object,
            new Mock<ILogger<WebhooksController>>().Object,
            postServiceMock.Object);

        var payload = new CreateBulletinPostWebhookRequest { Title = "Weekly News", Body = "Content" };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Api-Key"] = "wrong-key";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.ReceiveBulletinPost(payload, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Posts_CreatePost_ShouldReturnCreated_AndSaveToDb()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Announcing Intranet",
            Body = "Exciting news!",
            CreatedAt = DateTime.UtcNow
        };

        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.CreatePostAsync(userId, "Announcing Intranet", "Exciting news!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var controller = new BulletinPostController(postServiceMock.Object);
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var dto = new CreateBulletinPostDto { Title = "Announcing Intranet", Body = "Exciting news!" };

        // Act
        var result = await controller.CreatePost(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<BulletinPostResponseDto>(createdResult.Value);
        Assert.Equal(post.Id, returned.Id);
        Assert.Equal("Announcing Intranet", returned.Title);
        postServiceMock.Verify(s => s.CreatePostAsync(userId, "Announcing Intranet", "Exciting news!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Posts_CreatePost_ShouldReturnUnauthorized_WhenUserContextIsInvalid()
    {
        // Arrange
        var postServiceMock = new Mock<IBulletinPostService>();
        var controller = new BulletinPostController(postServiceMock.Object);
        
        var claims = new List<Claim>(); // No claims
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var dto = new CreateBulletinPostDto { Title = "Announcing Intranet", Body = "Exciting news!" };

        // Act
        var result = await controller.CreatePost(dto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Posts_GetPosts_ShouldReturnOkWithList()
    {
        // Arrange
        var posts = new List<BulletinPost>
        {
            new() { Id = Guid.NewGuid(), Title = "Post 1", Body = "Body 1", CreatedAt = DateTime.UtcNow }
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.GetPostsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var controller = new BulletinPostController(postServiceMock.Object);

        // Act
        var result = await controller.GetPosts(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<BulletinPostResponseDto>>(okResult.Value);
        Assert.Single(returned);
        Assert.Equal("Post 1", returned[0].Title);
        postServiceMock.Verify(s => s.GetPostsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Posts_UpdatePost_ShouldAllowAuthorToEditOwnPost()
    {
        var authorId = Guid.NewGuid();
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = authorId,
            Title = "Original",
            Body = "Original body",
            CreatedAt = DateTime.UtcNow
        };
        var updated = new BulletinPost
        {
            Id = post.Id,
            UserId = authorId,
            Title = "Updated",
            Body = "Updated body",
            CreatedAt = post.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.GetPostByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        postServiceMock.Setup(s => s.UpdatePostAsync(post.Id, "Updated", "Updated body", It.IsAny<CancellationToken>())).ReturnsAsync(updated);
        var controller = CreatePostsController(postServiceMock, authorId, "Employee");

        var result = await controller.UpdatePost(
            post.Id,
            new UpdateBulletinPostDto { Title = "Updated", Body = "Updated body" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BulletinPostResponseDto>(okResult.Value);
        Assert.Equal("Updated", response.Title);
        postServiceMock.Verify(s => s.UpdatePostAsync(post.Id, "Updated", "Updated body", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Posts_UpdatePost_ShouldForbidOtherStaffMember()
    {
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Original",
            Body = "Original body",
            CreatedAt = DateTime.UtcNow
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.GetPostByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        var controller = CreatePostsController(postServiceMock, Guid.NewGuid(), "Employee");

        var result = await controller.UpdatePost(
            post.Id,
            new UpdateBulletinPostDto { Title = "Updated" },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        postServiceMock.Verify(s => s.UpdatePostAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Posts_DeletePost_ShouldAllowAdminToDeleteAnyPost()
    {
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = authorId,
            Title = "Post",
            Body = "Body",
            CreatedAt = DateTime.UtcNow
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.GetPostByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        postServiceMock.Setup(s => s.DeletePostAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = CreatePostsController(postServiceMock, adminId, "Admin");

        var result = await controller.DeletePost(post.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        postServiceMock.Verify(s => s.DeletePostAsync(post.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Posts_DeletePost_ShouldAllowAuthorToDeleteOwnPost()
    {
        var authorId = Guid.NewGuid();
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = authorId,
            Title = "Post",
            Body = "Body",
            CreatedAt = DateTime.UtcNow
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.GetPostByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        postServiceMock.Setup(s => s.DeletePostAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = CreatePostsController(postServiceMock, authorId, "Employee");

        var result = await controller.DeletePost(post.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        postServiceMock.Verify(s => s.DeletePostAsync(post.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Posts_DeletePost_ShouldForbidOtherStaffMember()
    {
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            UserId = authorId,
            Title = "Post",
            Body = "Body",
            CreatedAt = DateTime.UtcNow
        };
        var postServiceMock = new Mock<IBulletinPostService>();
        postServiceMock.Setup(s => s.GetPostByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        var controller = CreatePostsController(postServiceMock, otherUserId, "Employee");

        var result = await controller.DeletePost(post.Id, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        postServiceMock.Verify(s => s.DeletePostAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BulletinPostController CreatePostsController(
        Mock<IBulletinPostService> postServiceMock,
        Guid userId,
        string role)
    {
        var controller = new BulletinPostController(postServiceMock.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    [Fact]
    public async Task FeedController_GetFeeds_ShouldReturnOkWithList()
    {
        // Arrange
        var feeds = new List<FeedItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Feed 1", Link = "https://link" }
        };
        var feedServiceMock = new Mock<IFeedService>();
        feedServiceMock.Setup(s => s.GetFeedsAsync(It.IsAny<GetFeedsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeds);

        var controller = new FeedController(feedServiceMock.Object);

        // Act
        var result = await controller.GetFeeds(new GetFeedsRequest { FeedSourceId = null, Page = 1, PageSize = 10 }, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<FeedItem>>(okResult.Value);
        Assert.Single(returned);
        feedServiceMock.Verify(s => s.GetFeedsAsync(It.Is<GetFeedsRequest>(r => r.FeedSourceId == null && r.Page == 1 && r.PageSize == 10), It.IsAny<CancellationToken>()), Times.Once);
    }


}
