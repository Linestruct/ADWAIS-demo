// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Persistence;

public class ApplicationDbContextFactory(IDbContextFactory<AnalyticsDbContext> factory) 
    : IApplicationDbContextFactory
{
    public IApplicationDbContext CreateDbContext() => factory.CreateDbContext();

    public async Task<IApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await factory.CreateDbContextAsync(cancellationToken);
    }
}
