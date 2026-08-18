// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class FeedService(IApplicationDbContext dbContext, ICurrentAccess currentAccess) : IFeedService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    public async Task<IEnumerable<FeedItem>> GetFeedsAsync(GetFeedsRequest request, CancellationToken ct = default)
    {
        var filter = OrganizationFilter.From(_currentAccess.Scope);
        if (filter.Denied) return [];

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _dbContext.FeedItems
            .Include(fi => fi.FeedSource)
            .AsNoTracking();

        if (filter.OrganizationId is { } orgId)
            query = query.Where(fi => fi.FeedSource != null && fi.FeedSource.OrganizationId == orgId);

        if (request.FeedSourceId.HasValue)
        {
            query = query.Where(fi => fi.FeedSourceId == request.FeedSourceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AuthorName))
        {
            var authorLower = request.AuthorName.ToLower();
            query = query.Where(fi => fi.Author != null && fi.Author.ToLower().Contains(authorLower));
        }

        return await query
            .OrderByDescending(fi => fi.PublishDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
