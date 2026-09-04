// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Api.DTOs.Kiosk;

public class KioskTokenResponseDto
{
    public required string Token { get; set; }
    public int ExpiresInHours { get; set; }
}
