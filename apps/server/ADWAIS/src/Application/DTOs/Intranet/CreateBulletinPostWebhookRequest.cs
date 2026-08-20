// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data accepted by the bulletin post webhook.
/// </summary>
public record CreateBulletinPostWebhookRequest
{
    /// <summary>
    /// The post title shown on the bulletin board.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The post body.
    /// </summary>
    public required string Body { get; set; }
}
