// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
