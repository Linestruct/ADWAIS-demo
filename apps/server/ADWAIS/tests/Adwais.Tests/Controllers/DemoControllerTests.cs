// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.Controllers.Authentication;
using Adwais.Api.DTOs.Kiosk;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class DemoControllerTests
{
    [Fact]
    public void GetDemoToken_ReturnsNotFound_WhenDemoAccessIsDisabled()
    {
        var tokenService = new Mock<ITokenService>();
        var controller = new DemoController(tokenService.Object, CreateConfiguration(false));

        var result = controller.GetDemoToken();

        Assert.IsType<NotFoundResult>(result.Result);
        tokenService.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetDemoToken_ReturnsViewerToken_WhenDemoAccessIsEnabled()
    {
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.GenerateKioskToken("demo-visitor", "Viewer"))
            .Returns("demo-token");
        var controller = new DemoController(tokenService.Object, CreateConfiguration(true));

        var result = controller.GetDemoToken();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<KioskTokenResponseDto>(ok.Value);
        Assert.Equal("demo-token", response.Token);
        Assert.Equal(30, response.ExpiresInDays);
    }

    private static IConfiguration CreateConfiguration(bool enabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:EnableDemoAccess"] = enabled.ToString()
            })
            .Build();
    }
}
