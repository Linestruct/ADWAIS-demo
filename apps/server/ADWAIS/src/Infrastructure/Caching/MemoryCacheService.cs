// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Adwais.Infrastructure.Caching;

public class MemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    public bool TryGetValue<T>(string key, out T? value)
    {
        return memoryCache.TryGetValue(key, out value);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpiration)
    {
        memoryCache.Set(key, value, absoluteExpiration);
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);
    }
}
