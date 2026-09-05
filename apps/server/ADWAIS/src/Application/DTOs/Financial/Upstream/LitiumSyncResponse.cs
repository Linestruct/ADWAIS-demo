// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Financial.Upstream;

public class LitiumSyncResponse
{
    public int? TotalOrders { get; set; }
    public List<LitiumOrderDto?>? Orders { get; set; } = new();

    public class LitiumOrderDto
    {
        public required Guid Id { get; set; }
        public required string OrderNumber { get; set; }
        public required DateTimeOffset CreatedDate { get; set; }
        public required string OrderStatus { get; set; }
        public decimal? TotalValueIncludingVat { get; set; }
        public decimal? TotalValueExcludingVat { get; set; }
        public string? Currency { get; set; } = string.Empty;
    }
}

