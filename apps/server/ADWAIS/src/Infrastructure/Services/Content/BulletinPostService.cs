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
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class BulletinPostService(IApplicationDbContext dbContext, ICurrentAccess currentAccess) : IBulletinPostService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    private OrganizationFilter OrganizationFilter => OrganizationFilter.From(_currentAccess.Scope);

    public async Task<BulletinPost?> GetPostByIdAsync(Guid id, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        var query = _dbContext.BulletinPosts.Where(post => post.Id == id);
        if (filter.Denied) return null;
        if (filter.OrganizationId is { } orgId) query = query.Where(post => post.OrganizationId == orgId);

        var post = await query.SingleOrDefaultAsync(ct);
        if (post != null)
        {
            post.User = await _dbContext.Users.SingleOrDefaultAsync(user => user.Id == post.UserId, ct);
        }

        return post;
    }

    public Task<BulletinPost> CreatePostAsync(
        Guid userId,
        string title,
        string body,
        CancellationToken ct = default)
        => CreatePostAsync(userId, title, body, organizationId: null, ct);

    public async Task<BulletinPost> CreatePostAsync(
        Guid userId,
        string title,
        string body,
        Guid? organizationId,
        CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        Guid targetOrgId;
        if (organizationId.HasValue)
        {
            var organizationExists = await _dbContext.Organizations
                .AnyAsync(org => org.Id == organizationId.Value, ct);
            if (!organizationExists)
            {
                throw new KeyNotFoundException($"Organization {organizationId.Value} does not exist.");
            }
            if (!filter.Denied && filter.OrganizationId is { } filterOrg && filterOrg != organizationId.Value)
            {
                throw new UnauthorizedAccessException("Cannot create bulletin post for another organization.");
            }
            targetOrgId = organizationId.Value;
        }
        else
        {
            if (filter.Denied) throw new UnauthorizedAccessException("The current scope cannot create bulletin posts.");
            if (filter.OrganizationId is null) throw new InvalidOperationException("Bulletin posts require an organization scope.");
            targetOrgId = filter.OrganizationId.Value;
        }

        var post = new BulletinPost
        {
            Id = Guid.NewGuid(),
            OrganizationId = targetOrgId,
            UserId = userId,
            Title = title,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.BulletinPosts.Add(post);
        await _dbContext.SaveChangesAsync(ct);

        return await GetPostByIdAsync(post.Id, ct) ?? post;
    }

    public async Task<IEnumerable<BulletinPost>> GetPostsAsync(CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        if (filter.Denied) return [];

        var query = _dbContext.BulletinPosts
            .Include(p => p.User)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();
        if (filter.OrganizationId is { } orgId) query = query.Where(p => p.OrganizationId == orgId);

        return await query.ToListAsync(ct);
    }

    public async Task<BulletinPost?> UpdatePostAsync(
        Guid id,
        string? title,
        string? body,
        CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        var query = _dbContext.BulletinPosts.Where(p => p.Id == id);
        if (filter.Denied) return null;
        if (filter.OrganizationId is { } orgId) query = query.Where(p => p.OrganizationId == orgId);

        var post = await query.SingleOrDefaultAsync(ct);
        if (post == null) return null;

        if (title != null) post.Title = title;
        if (body != null) post.Body = body;
        post.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return await GetPostByIdAsync(post.Id, ct) ?? post;
    }

    public async Task<bool> DeletePostAsync(Guid id, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        var query = _dbContext.BulletinPosts.Where(p => p.Id == id);
        if (filter.Denied) return false;
        if (filter.OrganizationId is { } orgId) query = query.Where(p => p.OrganizationId == orgId);

        var post = await query.SingleOrDefaultAsync(ct);
        if (post == null) return false;

        _dbContext.BulletinPosts.Remove(post);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
