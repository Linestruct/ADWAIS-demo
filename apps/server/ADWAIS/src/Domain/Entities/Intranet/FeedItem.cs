// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities.Intranet;

public class FeedItem
{
    public Guid Id { get; set; }
    public Guid? FeedSourceId { get; set; }
    public FeedSource? FeedSource { get; set; }
    public required string Title { get; set; }
    public string? Content { get; set; }
    public required string Link { get; set; }
    public DateTime PublishDate { get; set; }
    public string? Author { get; set; }
    public string? ImageUrl { get; set; }
}
