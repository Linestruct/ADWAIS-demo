// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.Controllers.Integrations;
using Adwais.Application.DTOs.Integrations;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Adwais.Tests.Controllers;

public class IntegrationControllerTests
{
    [Fact]
    public void GetOrderProviders_ReturnsAdapterDescriptors()
    {
        var source = new Mock<IOrderSource>();
        source.SetupGet(x => x.Configuration).Returns(new ProviderDescriptor(
            "litium", "Litium", [new("endpointUrl", "Endpoint URL", "url", true)]));
        var controller = new IntegrationController([source.Object], []);

        var result = controller.GetOrderProviders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var provider = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ProviderDescriptor>>(ok.Value));
        Assert.Equal("litium", provider.Id);
        Assert.Equal("endpointUrl", Assert.Single(provider.Settings).Key);
    }

    [Fact]
    public void GetMonitoringProviders_ReturnsAdapterDescriptors()
    {
        var provider = new Mock<IMonitoringProvider>();
        provider.SetupGet(x => x.Configuration).Returns(new ProviderDescriptor(
            "uptimerobot", "UptimeRobot", [new("apiKey", "API Key", "password", true)]));
        var controller = new IntegrationController([], [provider.Object]);

        var result = controller.GetMonitoringProviders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("uptimerobot", Assert.Single(Assert.IsAssignableFrom<IEnumerable<ProviderDescriptor>>(ok.Value)).Id);
    }
}
