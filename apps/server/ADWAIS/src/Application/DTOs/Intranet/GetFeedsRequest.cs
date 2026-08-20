// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Application.DTOs.Intranet;

public record GetFeedsRequest
{
    public Guid? FeedSourceId { get; init; }
    public string? AuthorName { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
