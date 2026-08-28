// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities;

public class MaterializedViewDirty
{
    public Guid OrganizationId { get; set; }
    public DateTimeOffset MarkedAt { get; set; }
}