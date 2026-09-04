// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities;

namespace Adwais.Application.Interfaces;

public enum OrganizationDeleteResult
{
    Deleted,
    NotFound,
    RefusedDefaultOrganization
}

public record OrganizationSummary(Guid Id, string Name, int MemberCount, int MonitorCount);

/// <summary>
/// Organization lifecycle: read, create, rename, and hard delete with
/// cascade. Deleting the default organization is always refused.
/// </summary>
public interface IOrganizationService
{
    Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<OrganizationSummary>> GetOrganizationSummariesAsync(CancellationToken ct = default);

    Task<Organization> CreateOrganizationAsync(string name, CancellationToken ct = default);

    Task<bool> RenameOrganizationAsync(Guid id, string name, CancellationToken ct = default);

    Task<OrganizationDeleteResult> DeleteOrganizationAsync(Guid id, CancellationToken ct = default);
}
