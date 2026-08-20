// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities.Intranet;

public class FeedSource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastPolledAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastSyncError { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<FeedItem> FeedItems { get; set; } = new List<FeedItem>();
}
