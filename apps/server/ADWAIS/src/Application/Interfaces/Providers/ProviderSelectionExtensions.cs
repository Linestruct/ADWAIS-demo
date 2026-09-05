// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Interfaces;

public static class ProviderSelectionExtensions
{
    public static IOrderSource ForProvider(this IEnumerable<IOrderSource> sources, string provider)
        => sources.SingleOrDefault(source => source.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"No order source is registered for provider '{provider}'.");

    public static IMonitoringProvider ForProvider(this IEnumerable<IMonitoringProvider> providers, string provider)
        => providers.SingleOrDefault(candidate => candidate.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"No monitoring provider is registered for provider '{provider}'.");
}
