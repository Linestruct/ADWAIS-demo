// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Interfaces;

public interface IApplicationDbContextFactory
{
    IApplicationDbContext CreateDbContext();
    Task<IApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
