// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Reflection;
using Adwais.Api.Controllers;
using Adwais.Api.Controllers.Administration;
using Microsoft.AspNetCore.Authorization;

namespace Adwais.Tests.Controllers;

public class BackgroundJobControllerTests
{
    [Theory]
    [InlineData(nameof(BackgroundJobController.TriggerMaterialViewRefresh))]
    [InlineData(nameof(BackgroundJobController.TriggerMonitoringMaterialViewRefresh))]
    public void MaterializedViewRefreshEndpointsRequireStaffAccess(string actionName)
    {
        var action = typeof(BackgroundJobController).GetMethod(actionName);
        var authorization = action?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("StaffAccess", authorization.Policy);
    }
}
