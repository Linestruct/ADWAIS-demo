// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Kiosk;

public class KioskTokenResponseDto
{
    public required string Token { get; set; }
    public int ExpiresInDays { get; set; }
}
