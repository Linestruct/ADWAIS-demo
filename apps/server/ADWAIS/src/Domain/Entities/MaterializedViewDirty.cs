// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities;

public class MaterializedViewDirty
{
    public Guid OrganizationId { get; set; }
    public DateTimeOffset MarkedAt { get; set; }
}