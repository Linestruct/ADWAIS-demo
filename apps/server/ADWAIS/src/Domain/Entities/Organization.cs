// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserAccess> UserAccesses { get; set; } = [];
    public ICollection<Tenant> Tenants { get; set; } = [];
}
