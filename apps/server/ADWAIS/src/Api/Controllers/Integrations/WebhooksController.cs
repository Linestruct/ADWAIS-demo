// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Financial.Upstream;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adwais.Api.Controllers.Integrations;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController(
    IOrderIngestionService ingestionService,
    IConfiguration configuration,
    ILogger<WebhooksController> logger,
    IBulletinPostService postService)
    : ControllerBase
{
    [HttpPost("motastic/{tenantId}")]
    public async Task<IActionResult> ReceiveMotasticWebhook(
        [FromRoute] Guid tenantId,
        [FromHeader(Name = "X-Api-Key")] string? apiKey,
        [FromBody] LitiumSyncResponse.LitiumOrderDto? payload,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey != configuration["Webhooks:MotasticApiKey"])
        {
            return Unauthorized();
        }

        if (payload == null)
        {
            return BadRequest(new { Error = "Payload cannot be null." });
        }

        try
        {
            await ingestionService.IngestSingleOrderAsync(tenantId, IntegrationProviders.Litium, LitiumOrderSource.Normalize(payload), ct);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ingest webhook for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error during webhook ingestion");
        }
    }

    [HttpPost("bulletin-posts")]
    public async Task<IActionResult> ReceiveBulletinPost(
        [FromBody] CreateBulletinPostWebhookRequest? payload,
        CancellationToken ct)
    {
        var apiKey = Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(apiKey) || apiKey != configuration["Webhooks:BulletinPostApiKey"])
        {
            return Unauthorized();
        }

        if (payload == null)
        {
            return BadRequest(new { Error = "Payload cannot be null." });
        }

        var post = await postService.CreatePostAsync(AnalyticsDbContext.SystemUserGuid, payload.Title, payload.Body, ct);

        return Ok(new { Id = post.Id });
    }
}
