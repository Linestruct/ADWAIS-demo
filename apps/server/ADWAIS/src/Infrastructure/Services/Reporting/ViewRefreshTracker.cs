// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services.Reporting;

public sealed class ViewRefreshTracker(IApplicationDbContext dbContext) : IViewRefreshTracker
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    public async Task MarkDirtyAsync(Guid organizationId, CancellationToken ct = default)
    {
        var existing = await _dbContext.MaterializedViewDirty
            .FirstOrDefaultAsync(e => e.OrganizationId == organizationId, ct);
        if (existing is not null) return;

        _dbContext.MaterializedViewDirty.Add(new MaterializedViewDirty
        {
            OrganizationId = organizationId,
            MarkedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> IsDirtyAsync(CancellationToken ct = default)
    {
        return await _dbContext.MaterializedViewDirty.AnyAsync(ct);
    }

    public async Task ClearDirtyAsync(CancellationToken ct = default)
    {
        var rows = await _dbContext.MaterializedViewDirty.ToListAsync(ct);
        _dbContext.MaterializedViewDirty.RemoveRange(rows);
        await _dbContext.SaveChangesAsync(ct);
    }
}