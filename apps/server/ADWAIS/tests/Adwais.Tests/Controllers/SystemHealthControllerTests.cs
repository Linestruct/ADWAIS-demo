// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Api.Controllers.System;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Adwais.Tests.Controllers;

public class SystemHealthControllerTests
{
    [Fact]
    public void GetHealth_RequiresPlatformAdmin()
    {
        var action = typeof(SystemHealthController).GetMethod(nameof(SystemHealthController.GetHealth));
        var authorization = action?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("PlatformAdminOnly", authorization.Policy);
    }

    [Fact]
    public void GetRecentJobs_RequiresAdminAccess()
    {
        var action = typeof(SystemHealthController).GetMethod(nameof(SystemHealthController.GetRecentJobs));
        var authorization = action?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("AdminOnly", authorization.Policy);
    }
}