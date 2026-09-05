// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data for creating a bulletin board post.
/// </summary>
public record CreateBulletinPostDto
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
