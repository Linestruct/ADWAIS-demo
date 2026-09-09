// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Reflection;
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
        Assert.Equal("PlatformDiagnosticsRead", authorization.Policy);
    }

}
