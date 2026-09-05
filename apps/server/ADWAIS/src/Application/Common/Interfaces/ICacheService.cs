// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Interfaces;

public interface ICacheService
{
    bool TryGetValue<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan absoluteExpiration);
    void Remove(string key);
}
