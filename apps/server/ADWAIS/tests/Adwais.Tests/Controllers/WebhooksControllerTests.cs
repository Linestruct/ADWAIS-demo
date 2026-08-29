// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Integrations;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class WebhooksControllerTests
{
    private readonly Mock<IOrderIngestionService> _ingestionServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IBulletinPostService> _postServiceMock;
    private readonly WebhooksController _controller;

    public WebhooksControllerTests()
    {
        _ingestionServiceMock = new Mock<IOrderIngestionService>();
        _configurationMock = new Mock<IConfiguration>();
        _postServiceMock = new Mock<IBulletinPostService>();
        _controller = new WebhooksController(
            _ingestionServiceMock.Object,
            new Mock<Adwais.Application.Common.Interfaces.IApplicationDbContext>().Object,
            _configurationMock.Object,
            NullLogger<WebhooksController>.Instance,
            _postServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private CreateBulletinPostWebhookRequest ValidPayload(Guid? organizationId = null) => new()
    {
        Title = "Webhook Title",
        Body = "Webhook Body",
        OrganizationId = organizationId
    };

    [Fact]
    public async Task ReceiveBulletinPost_MissingApiKey_ReturnsUnauthorized()
    {
        var result = await _controller.ReceiveBulletinPost(ValidPayload(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        _postServiceMock.Verify(
            s => s.CreatePostAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveBulletinPost_WrongApiKey_ReturnsUnauthorized()
    {
        _configurationMock.Setup(c => c["Webhooks:BulletinPostApiKey"]).Returns("correct-key");
        _controller.HttpContext.Request.Headers["X-Api-Key"] = "wrong-key";

        var result = await _controller.ReceiveBulletinPost(ValidPayload(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        _postServiceMock.Verify(
            s => s.CreatePostAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveBulletinPost_ValidRequest_CreatesPostWithOrganizationAndReturnsId()
    {
        var orgId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        _configurationMock.Setup(c => c["Webhooks:BulletinPostApiKey"]).Returns("correct-key");
        _controller.HttpContext.Request.Headers["X-Api-Key"] = "correct-key";
        _postServiceMock
            .Setup(s => s.CreatePostAsync(AnalyticsDbContext.SystemUserGuid, "Webhook Title", "Webhook Body", orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulletinPost { Id = postId, OrganizationId = orgId, Title = "Webhook Title", Body = "Webhook Body", CreatedAt = DateTime.UtcNow });

        var result = await _controller.ReceiveBulletinPost(ValidPayload(orgId), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(postId, okResult.Value?.GetType().GetProperty("Id")?.GetValue(okResult.Value));
    }

    [Fact]
    public async Task ReceiveBulletinPost_DefaultsToDefaultOrganization_WhenNoOrganizationProvided()
    {
        var postId = Guid.NewGuid();
        _configurationMock.Setup(c => c["Webhooks:BulletinPostApiKey"]).Returns("correct-key");
        _controller.HttpContext.Request.Headers["X-Api-Key"] = "correct-key";
        _postServiceMock
            .Setup(s => s.CreatePostAsync(
                AnalyticsDbContext.SystemUserGuid,
                "Webhook Title",
                "Webhook Body",
                AnalyticsDbContext.DefaultOrganizationGuid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulletinPost { Id = postId, OrganizationId = AnalyticsDbContext.DefaultOrganizationGuid, Title = "Webhook Title", Body = "Webhook Body", CreatedAt = DateTime.UtcNow });

        var result = await _controller.ReceiveBulletinPost(ValidPayload(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReceiveBulletinPost_UnknownOrganization_PropagatesNotFound()
    {
        var unknownOrgId = Guid.NewGuid();
        _configurationMock.Setup(c => c["Webhooks:BulletinPostApiKey"]).Returns("correct-key");
        _controller.HttpContext.Request.Headers["X-Api-Key"] = "correct-key";
        _postServiceMock
            .Setup(s => s.CreatePostAsync(
                AnalyticsDbContext.SystemUserGuid,
                "Webhook Title",
                "Webhook Body",
                unknownOrgId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Organization {unknownOrgId} does not exist."));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.ReceiveBulletinPost(ValidPayload(unknownOrgId), CancellationToken.None));
    }
}
