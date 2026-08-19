// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.DTOs.Intranet;

public record CreateBulletinPostWebhookRequest
{
    public required string Title { get; set; }
    public required string Body { get; set; }
    public Guid? OrganizationId { get; set; }
}
