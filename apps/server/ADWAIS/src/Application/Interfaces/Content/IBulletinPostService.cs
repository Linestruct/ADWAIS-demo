// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities.Intranet;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IBulletinPostService
{
    Task<BulletinPost?> GetPostByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<BulletinPost>> CreatePostAsync(Guid userId, string title, string body, CancellationToken ct = default);
    Task<BulletinPost> CreatePostAsync(Guid userId, string title, string body, Guid? organizationId, CancellationToken ct = default);
    Task<Result<BulletinPost>> UpdatePostAsync(Guid id, string? title, string? body, CancellationToken ct = default);
    Task<IEnumerable<BulletinPost>> GetPostsAsync(CancellationToken ct = default);
    Task<Result> DeletePostAsync(Guid id, CancellationToken ct = default);
}
