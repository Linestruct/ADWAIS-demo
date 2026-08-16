// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities.Intranet;

public class FeedSource
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastPolledAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastSyncError { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<FeedItem> FeedItems { get; set; } = new List<FeedItem>();
    public Organization? Organization { get; set; }
}
