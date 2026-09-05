// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Domain.Entities;

public class UserAccess
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? TenantId { get; set; }
    public required UserRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public Organization? Organization { get; set; }
    public Tenant? Tenant { get; set; }
}
