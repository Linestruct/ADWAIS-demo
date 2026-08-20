// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities.Intranet;

namespace Adwais.Application.Interfaces;

public interface IBulletinPostService
{
    Task<BulletinPost?> GetPostByIdAsync(Guid id, CancellationToken ct = default);
    Task<BulletinPost> CreatePostAsync(Guid userId, string title, string body, CancellationToken ct = default);
    Task<BulletinPost?> UpdatePostAsync(Guid id, string? title, string? body, CancellationToken ct = default);
    Task<IEnumerable<BulletinPost>> GetPostsAsync(CancellationToken ct = default);
    Task<bool> DeletePostAsync(Guid id, CancellationToken ct = default);
}
