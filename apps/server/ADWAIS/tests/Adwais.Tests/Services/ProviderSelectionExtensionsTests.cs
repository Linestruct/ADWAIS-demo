// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Moq;

namespace Adwais.Tests.Services;

public class ProviderSelectionExtensionsTests
{
    [Fact]
    public void ForProvider_SelectsTheMatchingOrderSource()
    {
        var litium = new Mock<IOrderSource>();
        litium.SetupGet(source => source.Provider).Returns("litium");
        var other = new Mock<IOrderSource>();
        other.SetupGet(source => source.Provider).Returns("other");

        var selected = new IOrderSource[] { other.Object, litium.Object }.ForProvider("litium");

        Assert.Same(litium.Object, selected);
    }

    [Fact]
    public void ForProvider_ThrowsWhenNoMonitoringProviderIsRegistered()
    {
        var provider = new Mock<IMonitoringProvider>();
        provider.SetupGet(candidate => candidate.Provider).Returns("uptimerobot");

        Assert.Throws<InvalidOperationException>(() => new[] { provider.Object }.ForProvider("other"));
    }
}
