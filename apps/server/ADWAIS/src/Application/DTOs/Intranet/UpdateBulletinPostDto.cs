// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data for updating a bulletin board post.
/// </summary>
public record UpdateBulletinPostDto
{
    /// <summary>
    /// The replacement title, or <see langword="null"/> to keep the current title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The replacement body, or <see langword="null"/> to keep the current body.
    /// </summary>
    public string? Body { get; set; }
}
